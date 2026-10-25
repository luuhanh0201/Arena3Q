import { Injectable, OnModuleDestroy } from "@nestjs/common";
import Redis from "ioredis";
import { Queue } from "bullmq";
import { config } from "../config";
/** Optional infrastructure: no Redis or job results are simulated when the service is not configured. */
@Injectable()
export class Infrastructure implements OnModuleDestroy {
  private redis: Redis | null = null;
  private queue: Queue | null = null;
  async cacheGet(key: string): Promise<string | null> {
    if (!config.redisUrl) return null;
    return (await this.connection()).get(key);
  }
  async cacheSet(key: string, value: string, seconds = 30): Promise<void> {
    if (config.redisUrl)
      await (await this.connection()).set(key, value, "EX", seconds);
  }
  private async connection(): Promise<Redis> {
    if (!config.redisUrl) throw new Error("Redis chưa được cấu hình.");
    if (!this.redis) {
      this.redis = new Redis(config.redisUrl, {
        lazyConnect: true,
        maxRetriesPerRequest: 1,
        connectTimeout: 1500,
      });
      this.redis.on("error", () => {});
      await this.redis.connect();
    }
    return this.redis;
  }
  async enqueueAssetInspection(assetId: string): Promise<string> {
    if (!config.redisUrl)
      throw new Error("BullMQ chưa được cấu hình; không tạo job local giả.");
    const url = new URL(config.redisUrl);
    this.queue ??= new Queue("arena3q-asset-inspection", {
      connection: {
        host: url.hostname,
        port: Number(url.port || 6379),
        username: url.username ? decodeURIComponent(url.username) : undefined,
        password: url.password ? decodeURIComponent(url.password) : undefined,
        db: Number(url.pathname.slice(1) || 0),
        tls: url.protocol === "rediss:" ? {} : undefined,
        maxRetriesPerRequest: 1,
      },
    });
    const job = await this.queue.add(
      "inspect",
      { assetId },
      {
        attempts: 3,
        backoff: { type: "exponential", delay: 2000 },
        removeOnComplete: 100,
        removeOnFail: 100,
      },
    );
    return job.id!;
  }
  async onModuleDestroy() {
    await this.queue?.close();
    this.redis?.disconnect();
  }
}
