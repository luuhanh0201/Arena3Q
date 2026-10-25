import {
  BadRequestException,
  CanActivate,
  Controller,
  ExecutionContext,
  Get,
  HttpCode,
  Inject,
  Injectable,
  Post,
  Req,
  Res,
  SetMetadata,
  UnauthorizedException,
  ForbiddenException,
} from "@nestjs/common";
import { Reflector } from "@nestjs/core";
import { Throttle } from "@nestjs/throttler";
import type { Request, Response } from "express";
import { parse, serialize } from "cookie";
import { createHash, randomBytes, randomUUID } from "node:crypto";
import {
  allows,
  loginSchema,
  type CurrentUser,
  type AdminUser,
  type Audit,
} from "@arena3q/contracts";
import { ADMIN_STORE, type AdminStore } from "../persistence/store";
import { hashPassword, verifyPassword } from "./password";
import { config } from "../config";
export const PUBLIC = "public";
export const PERMISSIONS = "permissions";
export const Public = () => SetMetadata(PUBLIC, true);
export const Permissions = (...permissions: string[]) =>
  SetMetadata(PERMISSIONS, permissions);
export interface AdminRequest extends Request {
  user: CurrentUser;
  requestId: string;
  sessionExpiresAt: string;
}
export const COOKIE = "arena3q_admin_session";
const digest = (value: string) =>
  createHash("sha256").update(value).digest("hex");
export const publicUser = ({
  passwordHash,
  active,
  ...user
}: AdminUser): CurrentUser => user;
export function auditEntry(
  request: AdminRequest,
  action: string,
  result: Audit["result"],
  actorId: string | null = null,
): Audit {
  return {
    id: randomUUID(),
    actorId,
    action,
    resourceType: "ADMIN_SESSION",
    resourceId: null,
    timestamp: new Date().toISOString(),
    result,
    requestId: request.requestId ?? randomUUID(),
    metadata: {
      ip: request.ip ?? null,
      userAgent: request.headers["user-agent"]?.slice(0, 300) ?? null,
      environment: process.env.NODE_ENV ?? "development",
    },
  };
}
@Injectable()
export class SessionGuard implements CanActivate {
  constructor(
    @Inject(ADMIN_STORE) private readonly store: AdminStore,
    private readonly reflector: Reflector,
  ) {}
  async canActivate(context: ExecutionContext): Promise<boolean> {
    if (
      this.reflector.getAllAndOverride<boolean>(PUBLIC, [
        context.getHandler(),
        context.getClass(),
      ])
    )
      return true;
    const request = context.switchToHttp().getRequest<AdminRequest>();
    const token = parse(request.headers.cookie ?? "")[COOKIE];
    if (!token) throw new UnauthorizedException("Vui lòng đăng nhập Admin.");
    const state = await this.store.read();
    const session = state.sessions.find(
      (session) =>
        session.tokenHash === digest(token) &&
        Date.parse(session.expiresAt) > Date.now(),
    );
    const user = session
      ? state.users.find((user) => user.id === session.userId && user.active)
      : null;
    if (!user) throw new UnauthorizedException("Phiên đăng nhập đã hết hạn.");
    request.user = publicUser(user);
    request.sessionExpiresAt = session!.expiresAt;
    const permissions =
      this.reflector.getAllAndOverride<string[]>(PERMISSIONS, [
        context.getHandler(),
        context.getClass(),
      ]) ?? [];
    if (!allows(request.user, permissions))
      throw new ForbiddenException("Bạn không có quyền truy cập.");
    return true;
  }
}
@Controller("api/admin/v1/auth")
export class AuthController {
  private readonly dummyHash = hashPassword(randomBytes(24).toString("hex"));
  constructor(@Inject(ADMIN_STORE) private readonly store: AdminStore) {}
  @Public()
  @Post("login")
  @HttpCode(200)
  @Throttle({ default: { limit: 10, ttl: 60000 } })
  async login(
    @Req() request: AdminRequest,
    @Res({ passthrough: true }) response: Response,
  ) {
    const parsed = loginSchema.safeParse(request.body);
    if (!parsed.success)
      throw new BadRequestException("Email hoặc mật khẩu không hợp lệ.");
    const input = parsed.data;
    const state = await this.store.read();
    const user = state.users.find(
      (user) =>
        user.email.toLowerCase() === input.email.toLowerCase() && user.active,
    );
    const matches = await verifyPassword(
      input.password,
      user?.passwordHash ?? (await this.dummyHash),
    );
    if (!user || !matches) {
      await this.store.update((state) => {
        state.audit.push(auditEntry(request, "LOGIN_FAILED", "FAILURE"));
      });
      throw new UnauthorizedException("Email hoặc mật khẩu không đúng.");
    }
    const token = randomBytes(32).toString("base64url");
    const createdAt = new Date().toISOString();
    const expiresAt = new Date(
      Date.now() + config.sessionHours * 3600000,
    ).toISOString();
    await this.store.update((state) => {
      state.sessions = state.sessions.filter(
        (session) => Date.parse(session.expiresAt) > Date.now(),
      );
      state.sessions.push({
        id: randomUUID(),
        tokenHash: digest(token),
        userId: user.id,
        createdAt,
        expiresAt,
      });
      state.audit.push(auditEntry(request, "LOGIN", "SUCCESS", user.id));
    });
    response.setHeader(
      "Set-Cookie",
      serialize(COOKIE, token, {
        httpOnly: true,
        sameSite: "lax",
        secure: config.cookieSecure,
        path: "/",
        maxAge: config.sessionHours * 3600,
      }),
    );
    return { data: publicUser(user), meta: { isDemo: false }, error: null };
  }
  @Get("me") me(
    @Req() request: AdminRequest,
    @Res({ passthrough: true }) response: Response,
  ) {
    // Keep the browser cookie aligned with the fixed server expiry, including
    // sessions extended by an explicit local configuration migration.
    const token = parse(request.headers.cookie ?? "")[COOKIE];
    if (!token) throw new UnauthorizedException("Vui lòng đăng nhập Admin.");
    response.setHeader(
      "Set-Cookie",
      serialize(COOKIE, token, {
        httpOnly: true,
        sameSite: "lax",
        secure: config.cookieSecure,
        path: "/",
        maxAge: Math.max(
          0,
          Math.floor(
            (Date.parse(request.sessionExpiresAt) - Date.now()) / 1000,
          ),
        ),
      }),
    );
    return { data: request.user, meta: { isDemo: false }, error: null };
  }
  @Post("logout") @HttpCode(200) async logout(
    @Req() request: AdminRequest,
    @Res({ passthrough: true }) response: Response,
  ) {
    const token = parse(request.headers.cookie ?? "")[COOKIE];
    await this.store.update((state) => {
      state.sessions = state.sessions.filter(
        (session) => session.tokenHash !== digest(token ?? ""),
      );
      state.audit.push(
        auditEntry(request, "LOGOUT", "SUCCESS", request.user.id),
      );
    });
    response.setHeader(
      "Set-Cookie",
      serialize(COOKIE, "", {
        httpOnly: true,
        sameSite: "lax",
        secure: config.cookieSecure,
        path: "/",
        maxAge: 0,
      }),
    );
    return { data: { loggedOut: true }, meta: { isDemo: false }, error: null };
  }
}
