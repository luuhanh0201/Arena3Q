import {
  BadRequestException,
  Controller,
  ForbiddenException,
  Get,
  Inject,
  NotFoundException,
  Param,
  Query,
  Req,
} from "@nestjs/common";
import {
  adminMenu,
  allows,
  featureOverrides,
  filterHeroes,
  heroQuerySchema,
  heroTabs,
  type HeroTab,
  type Hero,
  type HeroProfile,
} from "@arena3q/contracts";
import { ADMIN_STORE, type AdminStore } from "../persistence/store";
import { Permissions, type AdminRequest } from "../auth/auth";
import { config } from "../config";
const profile = ({ sections, ...hero }: Hero): HeroProfile => hero;
@Controller("api/admin/v1")
export class FeaturesController {
  constructor(@Inject(ADMIN_STORE) private readonly store: AdminStore) {}
  @Get("features") async features() {
    const state = await this.store.read();
    return {
      data: { menu: adminMenu, overrides: featureOverrides, heroTabs },
      meta: { isDemo: false, source: state.source, storage: config.driver },
      error: null,
    };
  }
  @Get("hero-taxonomy")
  @Permissions("admin.heroes.read")
  async taxonomy() {
    const state = await this.store.read();
    return {
      data: state.taxonomy,
      meta: { isDemo: false, source: state.source },
      error: null,
    };
  }
  @Get("overview")
  @Permissions("admin.heroes.read")
  async overview() {
    const state = await this.store.read();
    return {
      data: {
        heroes: state.heroes.length,
        assets: state.assets.length,
        animations: state.heroes.reduce(
          (sum, hero) => sum + hero.coverage.animations,
          0,
        ),
        skills: state.heroes.reduce(
          (sum, hero) => sum + hero.coverage.skills,
          0,
        ),
        coverage: state.heroes.map((hero) => ({
          id: hero.id,
          name: hero.displayName,
          ...hero.coverage,
        })),
        infrastructure: {
          database: config.driver,
          redis: !!config.redisUrl,
          storage: config.storageDriver,
          addressables: "NOT_CONNECTED",
        },
      },
      meta: { isDemo: false, source: state.source },
      error: null,
    };
  }
}
@Controller("api/admin/v1/heroes")
@Permissions("admin.heroes.read")
export class HeroesController {
  constructor(@Inject(ADMIN_STORE) private readonly store: AdminStore) {}
  @Get() async list(@Query() input: unknown) {
    const parsed = heroQuerySchema.safeParse(input);
    if (!parsed.success)
      throw new BadRequestException({
        message: "Bộ lọc hoặc phân trang không hợp lệ.",
        details: parsed.error.flatten(),
      });
    const state = await this.store.read();
    const query = parsed.data;
    const result = filterHeroes(state.heroes, query);
    return {
      data: result.rows,
      meta: {
        page: query.page,
        pageSize: query.pageSize,
        total: result.total,
        pages: Math.ceil(result.total / query.pageSize),
        isDemo: false,
        source: state.source,
      },
      error: null,
    };
  }
  @Get(":id") async hero(@Param("id") id: string) {
    const state = await this.store.read();
    const hero = state.heroes.find((hero) => hero.id === id);
    if (!hero) throw new NotFoundException("Không tìm thấy tướng.");
    return {
      data: profile(hero),
      meta: { isDemo: false, source: state.source },
      error: null,
    };
  }
  @Get(":id/:section") async section(
    @Param("id") id: string,
    @Param("section") section: string,
    @Req() request: AdminRequest,
  ) {
    const state = await this.store.read();
    const hero = state.heroes.find((hero) => hero.id === id);
    if (!hero) throw new NotFoundException("Không tìm thấy tướng.");
    const tab = section === "assets" ? "media" : section;
    if (!heroTabs.includes(tab as HeroTab) || tab === "overview")
      throw new NotFoundException("Không tìm thấy nhóm dữ liệu.");
    if (
      ["media", "model", "rig", "animations"].includes(tab) &&
      !allows(request.user, ["admin.assets.read"])
    )
      throw new ForbiddenException("Bạn không có quyền xem tài nguyên.");
    const data =
      section === "assets" || section === "media"
        ? hero.sections.assets
            .map((id) => state.assets.find((asset) => asset.id === id))
            .filter(Boolean)
        : hero.sections[tab as keyof typeof hero.sections];
    return { data, meta: { isDemo: false, source: state.source }, error: null };
  }
}
