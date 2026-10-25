import {
  Controller,
  Get,
  Inject,
  Injectable,
  NotFoundException,
  Param,
  Query,
  Req,
  Res,
  StreamableFile,
  BadRequestException,
} from "@nestjs/common";
import { promises as fs, createReadStream } from "node:fs";
import path from "node:path";
import type { Request, Response } from "express";
import { S3Client, GetObjectCommand } from "@aws-sdk/client-s3";
import { getSignedUrl } from "@aws-sdk/s3-request-presigner";
import { ADMIN_STORE, type AdminStore } from "../persistence/store";
import { Permissions } from "../auth/auth";
import { config } from "../config";

@Injectable()
export class AssetStorage {
  private client: S3Client | null = null;
  async localPath(key: string): Promise<string> {
    let base: string;
    try {
      base = await fs.realpath(config.storageRoot);
    } catch {
      throw new NotFoundException("Kho file local chưa có.");
    }
    const candidate = path.resolve(base, key);
    const relative = path.relative(base, candidate);
    if (relative.startsWith("..") || path.isAbsolute(relative))
      throw new BadRequestException("Storage key không hợp lệ.");
    let real: string;
    try {
      real = await fs.realpath(candidate);
    } catch {
      throw new NotFoundException("File local chưa có.");
    }
    const actual = path.relative(base, real);
    if (actual.startsWith("..") || path.isAbsolute(actual))
      throw new BadRequestException("Storage key không hợp lệ.");
    return real;
  }
  async signedUrl(key: string): Promise<string> {
    if (!process.env.S3_BUCKET)
      throw new NotFoundException("Object storage chưa được cấu hình.");
    this.client ??= new S3Client({
      region: process.env.S3_REGION ?? "auto",
      endpoint: process.env.S3_ENDPOINT || undefined,
      forcePathStyle: process.env.S3_FORCE_PATH_STYLE === "true",
    });
    return getSignedUrl(
      this.client,
      new GetObjectCommand({ Bucket: process.env.S3_BUCKET, Key: key }),
      { expiresIn: 60 },
    );
  }
}
@Controller("api/admin/v1/assets")
@Permissions("admin.assets.read")
export class AssetsController {
  constructor(
    @Inject(ADMIN_STORE) private readonly store: AdminStore,
    private readonly storage: AssetStorage,
  ) {}
  @Get(":id") async metadata(@Param("id") id: string) {
    const state = await this.store.read();
    const asset = state.assets.find((asset) => asset.id === id);
    if (!asset) throw new NotFoundException("Không tìm thấy asset.");
    return { data: asset, meta: { isDemo: false }, error: null };
  }
  @Get(":id/content") async content(
    @Param("id") id: string,
    @Query("variant") variant: string | undefined,
    @Req() request: Request,
    @Res({ passthrough: true }) response: Response,
  ) {
    if (variant && variant !== "thumbnail")
      throw new BadRequestException("Biến thể không hợp lệ.");
    const asset = (await this.store.read()).assets.find(
      (asset) => asset.id === id,
    );
    if (!asset || !asset.available || !asset.browserCompatible)
      throw new NotFoundException("Chưa có preview tương thích cho asset này.");
    const key = variant === "thumbnail" ? asset.thumbnailKey : asset.storageKey;
    if (!key) throw new NotFoundException("Chưa có tài nguyên preview.");
    response.setHeader("Cache-Control", "private, max-age=60");
    response.setHeader("X-Content-Type-Options", "nosniff");
    if (config.storageDriver === "s3") {
      response.status(302);
      response.setHeader("Location", await this.storage.signedUrl(key));
      return;
    }
    const filename = await this.storage.localPath(key);
    const info = await fs.stat(filename);
    if (!info.isFile()) throw new NotFoundException("Không tìm thấy file.");
    response.setHeader(
      "Content-Type",
      asset.contentType ?? "application/octet-stream",
    );
    response.setHeader("Accept-Ranges", "bytes");
    const range = request.headers.range;
    if (range) {
      const match = /^bytes=(\d*)-(\d*)$/.exec(range);
      if (!match || (!match[1] && !match[2])) {
        response.status(416);
        response.setHeader("Content-Range", `bytes */${info.size}`);
        return;
      }
      const start = match[1]
        ? Number(match[1])
        : Math.max(0, info.size - Number(match[2]));
      const end = match[1]
        ? match[2]
          ? Number(match[2])
          : info.size - 1
        : info.size - 1;
      if (
        !Number.isSafeInteger(start) ||
        !Number.isSafeInteger(end) ||
        start < 0 ||
        end < start ||
        start >= info.size
      ) {
        response.status(416);
        response.setHeader("Content-Range", `bytes */${info.size}`);
        return;
      }
      const last = Math.min(end, info.size - 1);
      response.status(206);
      response.setHeader(
        "Content-Range",
        `bytes ${start}-${last}/${info.size}`,
      );
      response.setHeader("Content-Length", last - start + 1);
      return new StreamableFile(
        createReadStream(filename, { start, end: last }),
      );
    }
    response.setHeader("Content-Length", info.size);
    return new StreamableFile(createReadStream(filename));
  }
}
