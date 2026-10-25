import "reflect-metadata";
import { randomUUID } from "node:crypto";
import {
  BadRequestException,
  Catch,
  Controller,
  Get,
  HttpException,
  Module,
  ValidationPipe,
  type ArgumentsHost,
  type ExceptionFilter,
  type INestApplication,
} from "@nestjs/common";
import { APP_GUARD, NestFactory } from "@nestjs/core";
import { ThrottlerGuard, ThrottlerModule } from "@nestjs/throttler";
import type { NextFunction, Request, Response } from "express";
import helmet from "helmet";
import { ZodError } from "zod";
import { ADMIN_STORE, type AdminStore } from "./persistence/store";
import { JsonFileStore } from "./persistence/json-store";
import {
  postgresDataSource,
  PostgresStore,
} from "./persistence/postgres-store";
import { config } from "./config";
import {
  AuthController,
  Public,
  SessionGuard,
  type AdminRequest,
} from "./auth/auth";
import { FeaturesController, HeroesController } from "./heroes/heroes";
import { AssetStorage, AssetsController } from "./assets/assets";
import { Infrastructure } from "./integrations/integrations";
import {
  SkillEditorController,
  SkillEditorService,
} from "./heroes/skill-editor";

@Controller()
class HealthController {
  @Public() @Get("health") health() {
    return {
      status: "ok",
      service: "arena3q-admin-api",
      storage: config.driver,
    };
  }
}
@Catch()
class ApiExceptionFilter implements ExceptionFilter {
  catch(exception: unknown, host: ArgumentsHost) {
    const context = host.switchToHttp();
    const response = context.getResponse<Response>();
    const request = context.getRequest<AdminRequest>();
    const status =
      exception instanceof HttpException
        ? exception.getStatus()
        : exception instanceof ZodError
          ? 400
          : 500;
    const payload =
      exception instanceof HttpException ? exception.getResponse() : null;
    const message =
      status === 500
        ? "Không thể tải dữ liệu. Thử lại."
        : typeof payload === "string"
          ? payload
          : payload && typeof payload === "object" && "message" in payload
            ? String(payload.message)
            : "Yêu cầu không hợp lệ.";
    if (status === 500)
      console.error(
        `[${request.requestId}]`,
        exception instanceof Error ? exception.message : "Internal error",
      );
    response.status(status).json({
      data: null,
      meta: null,
      error: {
        code:
          (
            {
              400: "VALIDATION_ERROR",
              401: "UNAUTHENTICATED",
              403: "FORBIDDEN",
              404: "NOT_FOUND",
              409: "CONFLICT",
              429: "RATE_LIMITED",
            } as Record<number, string>
          )[status] ?? "INTERNAL_ERROR",
        message,
        requestId: request.requestId,
      },
    });
  }
}
export async function createApp(
  override?: AdminStore,
): Promise<INestApplication> {
  let store = override;
  if (!store) {
    if (config.driver === "postgres") {
      const source = postgresDataSource(config.databaseUrl);
      await source.initialize();
      store = new PostgresStore(source);
    } else if (config.driver === "json")
      store = new JsonFileStore(config.databaseFile);
    else throw new Error("DATABASE_DRIVER must be json or postgres.");
  }
  await store.read();
  @Module({
    imports: [ThrottlerModule.forRoot([{ ttl: 60000, limit: 300 }])],
    controllers: [
      HealthController,
      AuthController,
      FeaturesController,
      HeroesController,
      AssetsController,
      SkillEditorController,
    ],
    providers: [
      { provide: ADMIN_STORE, useValue: store },
      AssetStorage,
      Infrastructure,
      SkillEditorService,
      { provide: APP_GUARD, useClass: ThrottlerGuard },
      { provide: APP_GUARD, useClass: SessionGuard },
    ],
  })
  class AppModule {}
  const app = await NestFactory.create(AppModule, {
    logger: process.env.NODE_ENV === "test" ? false : ["error", "warn", "log"],
  });
  app.use(helmet({ crossOriginResourcePolicy: { policy: "same-site" } }));
  app.use((request: AdminRequest, response: Response, next: NextFunction) => {
    request.requestId = randomUUID();
    response.setHeader("X-Request-ID", request.requestId);
    if (request.path.startsWith("/api/admin"))
      response.setHeader("Cache-Control", "no-store");
    if (!["GET", "HEAD", "OPTIONS"].includes(request.method)) {
      const origin = request.headers.origin;
      if (
        (origin && !config.allowedOrigins.includes(origin)) ||
        request.headers["sec-fetch-site"] === "cross-site"
      )
        return response.status(403).json({
          data: null,
          meta: null,
          error: {
            code: "FORBIDDEN",
            message: "Nguồn yêu cầu không được phép.",
            requestId: request.requestId,
          },
        });
      if (!request.is("application/json"))
        return response.status(400).json({
          data: null,
          meta: null,
          error: {
            code: "VALIDATION_ERROR",
            message: "Yêu cầu cần JSON.",
            requestId: request.requestId,
          },
        });
    }
    next();
  });
  app.enableCors({ origin: config.allowedOrigins, credentials: true });
  app.useGlobalFilters(new ApiExceptionFilter());
  app.enableShutdownHooks();
  app.getHttpAdapter().getInstance().disable("x-powered-by");
  const originalClose = app.close.bind(app);
  app.close = async () => {
    await originalClose();
    await store?.close?.();
  };
  return app;
}
