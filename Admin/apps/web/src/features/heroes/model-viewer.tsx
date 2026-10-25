"use client";
import {
  Component,
  Suspense,
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { Canvas, useFrame, useThree } from "@react-three/fiber";
import { OrbitControls, useAnimations, useGLTF } from "@react-three/drei";
import {
  Box3,
  LoopOnce,
  LoopRepeat,
  Mesh,
  MeshStandardMaterial,
  PMREMGenerator,
  Vector3,
  type AnimationAction,
  type Material,
} from "three";
import { clone } from "three/examples/jsm/utils/SkeletonUtils.js";
import { RoomEnvironment } from "three/examples/jsm/environments/RoomEnvironment.js";
import {
  Camera,
  Grid2X2,
  Maximize2,
  Network,
  Pause,
  Play,
  RotateCcw,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { assetUrl, cn, formatNumber } from "@/lib/utils";
import { EmptyState } from "@/components/states";
interface Metadata {
  meshes: number;
  vertices: number;
  triangles: number;
  materials: number;
  bones: number;
  clips: string[];
}
class ViewerBoundary extends Component<
  { children: ReactNode },
  { error: boolean }
> {
  state = { error: false };
  static getDerivedStateFromError() {
    return { error: true };
  }
  render() {
    return this.state.error ? (
      <div className="p-5">
        <EmptyState
          title="Không thể mở preview 3D"
          description="File có thể không phải GLB hợp lệ hoặc không truy cập được. Metadata của tướng vẫn có thể xem ở bên cạnh."
        />
      </div>
    ) : (
      this.props.children
    );
  }
}
export default function ModelViewer({
  assetId,
  initialClip,
}: {
  assetId: string;
  initialClip?: string;
}) {
  const [supported, setSupported] = useState<boolean | null>(null);
  const [grid, setGrid] = useState(true);
  const [axes, setAxes] = useState(false);
  const [skeleton, setSkeleton] = useState(false);
  const [shading, setShading] = useState("textured");
  const [view, setView] = useState("perspective");
  const [reset, setReset] = useState(0);
  const [metadata, setMetadata] = useState<Metadata | null>(null);
  const [playing, setPlaying] = useState(true);
  const [loop, setLoop] = useState(true);
  const [speed, setSpeed] = useState(1);
  const [clip, setClip] = useState(initialClip ?? "");
  const [time, setTime] = useState(0);
  const [duration, setDuration] = useState(0);
  const [seek, setSeek] = useState<{ value: number; tick: number }>({
    value: 0,
    tick: 0,
  });
  const screenshot = useRef<(() => string) | null>(null);
  useEffect(() => {
    const canvas = document.createElement("canvas");
    const context = canvas.getContext("webgl2") ?? canvas.getContext("webgl");
    setSupported(!!context);
    (context as WebGLRenderingContext | null)
      ?.getExtension("WEBGL_lose_context")
      ?.loseContext();
  }, []);
  const loaded = useCallback((value: Metadata) => {
    setMetadata(value);
    setClip((current) =>
      value.clips.includes(current)
        ? current
        : value.clips.includes("Idle")
          ? "Idle"
          : (value.clips[0] ?? ""),
    );
  }, []);
  useEffect(() => {
    if (initialClip) setClip(initialClip);
  }, [initialClip]);
  if (supported === false)
    return (
      <EmptyState
        title="Trình duyệt chưa hỗ trợ WebGL"
        description="Hãy dùng trình duyệt có WebGL để xem model. Metadata tài nguyên vẫn được giữ nguyên."
      />
    );
  return (
    <ViewerBoundary key={assetId}>
      <div className="bg-[#0c1523]">
        <div className="flex flex-wrap items-center gap-2 border-b border-border p-3">
          <select
            aria-label="Góc nhìn model"
            value={view}
            onChange={(event) => setView(event.target.value)}
            className="h-8! text-xs!"
          >
            <option value="perspective">Góc 3/4</option>
            <option value="front">Trước</option>
            <option value="back">Sau</option>
            <option value="left">Trái</option>
            <option value="right">Phải</option>
          </select>
          <select
            aria-label="Shading model"
            value={shading}
            onChange={(event) => setShading(event.target.value)}
            className="h-8! text-xs!"
          >
            <option value="textured">Texture</option>
            <option value="solid">Solid</option>
            <option value="wireframe">Wireframe</option>
          </select>
          <Button
            title="Bật/tắt lưới"
            aria-pressed={grid}
            size="icon"
            variant={grid ? "secondary" : "ghost"}
            onClick={() => setGrid(!grid)}
          >
            <Grid2X2 />
          </Button>
          <Button
            title="Bật/tắt trục"
            aria-pressed={axes}
            size="icon"
            variant={axes ? "secondary" : "ghost"}
            onClick={() => setAxes(!axes)}
          >
            <Maximize2 />
          </Button>
          <Button
            title="Hiện skeleton"
            aria-pressed={skeleton}
            disabled={!metadata?.bones}
            size="icon"
            variant={skeleton ? "secondary" : "ghost"}
            onClick={() => setSkeleton(!skeleton)}
          >
            <Network />
          </Button>
          <Button
            title="Đặt lại camera"
            size="icon"
            variant="ghost"
            onClick={() => {
              setView("perspective");
              setReset((value) => value + 1);
            }}
          >
            <RotateCcw />
          </Button>
          <Button
            title="Chụp preview"
            size="icon"
            variant="ghost"
            disabled={!metadata}
            onClick={() => {
              const url = screenshot.current?.();
              if (url) {
                const anchor = document.createElement("a");
                anchor.href = url;
                anchor.download = "arena3q-model-preview.png";
                anchor.click();
              }
            }}
          >
            <Camera />
          </Button>
        </div>
        <div className="relative h-[440px] min-h-60">
          {supported && (
            <Canvas
              frameloop="demand"
              gl={{ preserveDrawingBuffer: true, antialias: true }}
              camera={{ position: [3, 2, 4], fov: 40 }}
              dpr={[1, 1.5]}
            >
              <color attach="background" args={["#0c1523"]} />
              <ambientLight intensity={1.8} />
              <directionalLight position={[5, 8, 6]} intensity={2.5} />
              <directionalLight position={[-4, 2, -5]} intensity={1.5} />
              {grid && <gridHelper args={[20, 20, "#34445d", "#1d2b40"]} />}
              {axes && <axesHelper args={[2]} />}
              <OrbitControls makeDefault enableDamping />
              <Suspense fallback={null}>
                <Scene
                  assetId={assetId}
                  view={view}
                  reset={reset}
                  shading={shading}
                  skeleton={skeleton}
                  clip={clip}
                  playing={playing}
                  loop={loop}
                  speed={speed}
                  seek={seek}
                  onLoaded={loaded}
                  onTime={setTime}
                  onDuration={setDuration}
                  screenshot={screenshot}
                />
              </Suspense>
            </Canvas>
          )}
          {supported && !metadata && (
            <div
              role="status"
              className="pointer-events-none absolute inset-0 flex items-center justify-center text-xs text-slate-300"
            >
              Đang tải model...
            </div>
          )}
        </div>
        {metadata && (
          <div className="flex flex-wrap gap-x-5 gap-y-2 border-t border-border px-4 py-3 text-[10px] text-muted-foreground">
            <span>{metadata.meshes} mesh</span>
            <span>{formatNumber(metadata.vertices)} đỉnh</span>
            <span>{formatNumber(metadata.triangles)} tam giác</span>
            <span>{metadata.materials} material</span>
            <span>{metadata.bones} xương</span>
            <span className="text-primary/70">
              Thông số preview trình duyệt
            </span>
          </div>
        )}
        {metadata?.clips.length ? (
          <div className="space-y-3 border-t border-border p-4">
            <div className="flex flex-wrap items-center gap-2">
              <Button
                size="icon"
                variant="outline"
                aria-label={playing ? "Tạm dừng animation" : "Phát animation"}
                onClick={() => setPlaying(!playing)}
              >
                {playing ? <Pause /> : <Play />}
              </Button>
              <select
                aria-label="Clip GLB"
                value={clip}
                onChange={(event) => setClip(event.target.value)}
                className="h-8! text-xs!"
              >
                {metadata.clips.map((name) => (
                  <option key={name}>{name}</option>
                ))}
              </select>
              <select
                aria-label="Tốc độ animation"
                value={speed}
                onChange={(event) => setSpeed(Number(event.target.value))}
                className="h-8! min-w-20! text-xs!"
              >
                {[0.25, 0.5, 1, 2].map((value) => (
                  <option key={value} value={value}>
                    {value}x
                  </option>
                ))}
              </select>
              <label className="ml-auto flex items-center gap-2 text-xs text-muted-foreground">
                <input
                  type="checkbox"
                  checked={loop}
                  onChange={(event) => setLoop(event.target.checked)}
                />
                Lặp
              </label>
            </div>
            <div className="flex items-center gap-3">
              <input
                type="range"
                aria-label="Timeline animation"
                min="0"
                max={duration || 1}
                step="0.01"
                value={Math.min(time, duration || 1)}
                className="min-w-0 flex-1 accent-amber-300"
                onChange={(event) => {
                  // Capture the DOM value before React restores this controlled input.
                  const value = Number(event.currentTarget.value);
                  setTime(value);
                  setSeek((current) => ({ value, tick: current.tick + 1 }));
                }}
              />
              <span className="min-w-24 text-right text-[10px] text-muted-foreground">
                {time.toFixed(2)} / {duration.toFixed(2)} s
              </span>
            </div>
            {initialClip && !metadata.clips.includes(initialClip) && (
              <p className="text-[10px] text-amber-300">
                GLB không có clip tên {initialClip}; hãy chọn một clip có trong
                file.
              </p>
            )}
          </div>
        ) : (
          metadata && (
            <p className="border-t border-border p-4 text-xs text-muted-foreground">
              GLB không có animation nhúng.
            </p>
          )
        )}
      </div>
    </ViewerBoundary>
  );
}
function Scene({
  assetId,
  view,
  reset,
  shading,
  skeleton,
  clip,
  playing,
  loop,
  speed,
  seek,
  onLoaded,
  onTime,
  onDuration,
  screenshot,
}: {
  assetId: string;
  view: string;
  reset: number;
  shading: string;
  skeleton: boolean;
  clip: string;
  playing: boolean;
  loop: boolean;
  speed: number;
  seek: { value: number; tick: number };
  onLoaded: (data: Metadata) => void;
  onTime: (value: number) => void;
  onDuration: (value: number) => void;
  screenshot: React.RefObject<(() => string) | null>;
}) {
  const gltf = useGLTF(assetUrl(assetId));
  const scene = useMemo(() => {
    const scene = clone(gltf.scene);
    scene.traverse((object) => {
      if (object instanceof Mesh)
        object.material = Array.isArray(object.material)
          ? object.material.map((material) => material.clone())
          : object.material.clone();
    });
    return scene;
  }, [gltf.scene]);
  const { actions, mixer } = useAnimations(gltf.animations, scene);
  const { camera, controls, invalidate, gl, scene: rootScene } = useThree();
  useEffect(() => {
    const generator = new PMREMGenerator(gl);
    const room = new RoomEnvironment();
    const environment = generator.fromScene(room, 0.04);
    const previous = rootScene.environment;
    rootScene.environment = environment.texture;
    invalidate();
    return () => {
      rootScene.environment = previous;
      environment.dispose();
      room.dispose();
      generator.dispose();
    };
  }, [gl, rootScene, invalidate]);
  const active = useRef<AnimationAction | null>(null);
  const last = useRef(0);
  useEffect(() => {
    let meshes = 0,
      vertices = 0,
      triangles = 0;
    const materials = new Set<Material>();
    const bones = new Set<string>();
    scene.traverse((object) => {
      if (object instanceof Mesh) {
        meshes++;
        vertices += object.geometry.attributes.position?.count ?? 0;
        triangles +=
          (object.geometry.index?.count ??
            object.geometry.attributes.position?.count ??
            0) / 3;
        (Array.isArray(object.material)
          ? object.material
          : [object.material]
        ).forEach((material) => materials.add(material));
      }
      if (object.type === "Bone") bones.add(object.uuid);
    });
    onLoaded({
      meshes,
      vertices,
      triangles,
      materials: materials.size,
      bones: bones.size,
      clips: gltf.animations.map((clip) => clip.name),
    });
    return () => {
      materials.forEach((material) => material.dispose());
    };
  }, [scene, gltf.animations, onLoaded]);
  useEffect(() => {
    // Include the actual skinned vertices and held weapon, rather than cached
    // bind-pose bounds, which can place the animated model outside the frame.
    scene.updateMatrixWorld(true);
    const box = new Box3().setFromObject(scene, true);
    const center = box.getCenter(new Vector3());
    const size = box.getSize(new Vector3());
    const radius = Math.max(size.x, size.y, size.z, 0.1) * 1.65;
    const direction = (
      {
        front: [0, 0.15, 1],
        back: [0, 0.15, -1],
        left: [-1, 0.15, 0],
        right: [1, 0.15, 0],
        perspective: [1, 0.5, 1],
      } as Record<string, number[]>
    )[view] ?? [1, 0.5, 1];
    camera.position
      .copy(center)
      .add(new Vector3(...direction).normalize().multiplyScalar(radius));
    camera.near = Math.max(radius / 1000, 0.001);
    camera.far = radius * 100;
    camera.updateProjectionMatrix();
    camera.lookAt(center);
    const orbit = controls as unknown as {
      target: Vector3;
      update: () => void;
    } | null;
    if (orbit) {
      orbit.target.copy(center);
      orbit.update();
    }
    invalidate();
  }, [scene, view, reset, camera, controls, invalidate]);
  useEffect(() => {
    scene.traverse((object) => {
      if (object instanceof Mesh) {
        for (const material of Array.isArray(object.material)
          ? object.material
          : [object.material]) {
          const m = material as MeshStandardMaterial;
          if (!("originalMap" in m.userData)) m.userData.originalMap = m.map;
          m.map = shading === "solid" ? null : m.userData.originalMap;
          m.wireframe = shading === "wireframe";
          m.needsUpdate = true;
        }
      }
    });
    invalidate();
  }, [scene, shading, invalidate]);
  useEffect(() => {
    active.current?.stop();
    active.current = actions[clip] ?? null;
    active.current?.reset().play();
    onDuration(active.current?.getClip().duration ?? 0);
    invalidate();
    return () => {
      active.current?.stop();
    };
  }, [actions, clip, onDuration, invalidate]);
  useEffect(() => {
    const action = active.current;
    if (action) {
      action.paused = !playing;
      action.setLoop(loop ? LoopRepeat : LoopOnce, loop ? Infinity : 1);
      action.clampWhenFinished = !loop;
    }
    mixer.timeScale = speed;
    invalidate();
  }, [playing, loop, speed, clip, mixer, invalidate]);
  useEffect(() => {
    if (active.current) {
      active.current.time = seek.value;
      mixer.update(0);
      onTime(seek.value);
      invalidate();
    }
  }, [seek, mixer, onTime, invalidate]);
  useEffect(() => {
    screenshot.current = () => {
      gl.render(rootScene, camera);
      return gl.domElement.toDataURL("image/png");
    };
    return () => {
      screenshot.current = null;
    };
  }, [gl, rootScene, camera, screenshot]);
  useFrame((state) => {
    if (playing && active.current) invalidate();
    if (state.clock.elapsedTime - last.current > 0.1) {
      last.current = state.clock.elapsedTime;
      onTime(active.current?.time ?? 0);
    }
  });
  return (
    <>
      <primitive object={scene} />
      {skeleton && <skeletonHelper args={[scene]} />}
    </>
  );
}
