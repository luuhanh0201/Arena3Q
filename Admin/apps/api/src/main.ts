import { createApp } from "./app";
import { config } from "./config";
async function bootstrap() {
  const app = await createApp();
  await app.listen(config.port, config.host);
  console.log(
    `Arena3Q Admin API: http://${config.host}:${config.port}/api/admin/v1`,
  );
}
bootstrap().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
