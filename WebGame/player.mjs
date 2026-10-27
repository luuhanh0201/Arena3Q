const $ = id => document.getElementById(id);
const storageKey = 'arena3q.web.display';
let settings, selected, instance, loading = false;
const area = $('play-area'), frame = $('game-frame'), canvas = $('unity-canvas');

function fit() {
  if (!selected) return;
  const style = getComputedStyle(area);
  const width = Math.max(1, area.clientWidth - parseFloat(style.paddingLeft) - parseFloat(style.paddingRight));
  const height = Math.max(1, area.clientHeight - parseFloat(style.paddingTop) - parseFloat(style.paddingBottom));
  const scale = Math.min(width / selected.width, height / selected.height);
  frame.style.width = `${selected.width * scale}px`;
  frame.style.height = `${selected.height * scale}px`;
  // Unity owns its backing buffer after startup. Keep its automatic resize enabled
  // and derive the pixel ratio from the fitted canvas to preserve the chosen size.
  if (instance) instance.Module.devicePixelRatio = selected.width / canvas.getBoundingClientRect().width;
}

function selectMode(id) {
  selected = settings.modes.find(mode => mode.id === id) || settings.modes.find(mode => mode.id === 'pc');
  try { localStorage.setItem(storageKey, selected.id); } catch { /* Storage can be disabled. */ }
  for (const button of $('modes').querySelectorAll('button')) {
    button.setAttribute('aria-pressed', String(button.dataset.mode === selected.id));
  }
  // Setting the backing buffer, rather than stretching an old screenshot, makes Unity lay out
  // its own cameras, safe areas, pointer coordinates and HUD for the selected aspect ratio.
  canvas.width = selected.width;
  canvas.height = selected.height;
  frame.dataset.mode = selected.id;
  $('mode-label').textContent = selected.label;
  fit();
  window.dispatchEvent(new Event('resize'));
  if (instance) canvas.focus({ preventScroll: true });
}

function progress(value) {
  const percent = Math.min(100, Math.max(0, Math.round(value * 100)));
  $('progress').style.width = `${percent}%`;
  $('progress').setAttribute('aria-valuenow', String(percent));
  $('percent').textContent = `${percent}%`;
  $('status').textContent = percent < 90 ? 'Đang tải tài nguyên. Vui lòng chờ một chút.' : 'Đang chuẩn bị vào game...';
}

function showFailure(error) {
  console.error('[Arena3Q Web]', error);
  loading = false;
  $('cover').hidden = false;
  $('loading').hidden = true;
  $('title').textContent = 'Chưa thể vào game';
  $('status').textContent = navigator.onLine
    ? 'Không thể mở game. Vui lòng thử lại hoặc dùng trình duyệt khác.'
    : 'Mất kết nối mạng. Kiểm tra kết nối rồi thử lại.';
  $('start').hidden = false;
  $('start').disabled = false;
  $('start').textContent = 'THỬ LẠI';
  $('start').onclick = () => location.reload();
}

async function start() {
  if (loading || instance) return;
  loading = true;
  $('start').hidden = true;
  $('loading').hidden = false;
  $('title').textContent = 'Đang vào game';
  progress(0);
  try {
    const probe = document.createElement('canvas');
    const graphics = probe.getContext('webgl2');
    if (!graphics) throw new Error('WebGL2 unavailable');
    graphics.getExtension('WEBGL_lose_context')?.loseContext();
    const response = await fetch('build.json', { cache: 'no-cache' });
    if (!response.ok) throw new Error(`Build manifest: HTTP ${response.status}`);
    const { loaderUrl, contentHash, startupScene, battleScene, ...build } = await response.json();
    await new Promise((resolve, reject) => {
      const script = document.createElement('script');
      script.src = loaderUrl;
      script.onload = resolve;
      script.onerror = () => reject(new Error('Game loader unavailable'));
      document.body.append(script);
    });
    instance = await window.createUnityInstance(canvas, {
      ...build,
      matchWebGLToCanvasSize: true,
      devicePixelRatio: selected.width / canvas.getBoundingClientRect().width,
      autoSyncPersistentDataPath: true,
      showBanner: (message, type) => {
        console[type === 'error' ? 'error' : 'warn']('[Unity]', message);
      }
    }, progress);
    fit();
    progress(1);
    frame.classList.add('running');
    $('cover').hidden = true;
    loading = false;
    // Preserve Unity's canvas and runtime when switching display modes.
    canvas.focus({ preventScroll: true });
    frame.dataset.ready = 'true';
  } catch (error) { showFailure(error); }
}

$('fullscreen').addEventListener('click', async () => {
  try {
    if (document.fullscreenElement) await document.exitFullscreen();
    else if (document.documentElement.requestFullscreen) await document.documentElement.requestFullscreen();
    else {
      $('hint').textContent = 'Xoay ngang thiết bị để mở rộng màn hình chơi.';
      $('hint').style.display = 'inline';
    }
  } catch (error) {
    console.warn('[Arena3Q Web] Fullscreen unavailable', error);
    $('hint').textContent = 'Trình duyệt chưa cho phép toàn màn hình. Bạn vẫn có thể chơi trong khung này.';
    $('hint').style.display = 'inline';
  }
});
document.addEventListener('fullscreenchange', () => {
  $('fullscreen').textContent = document.fullscreenElement ? 'Thu màn hình' : 'Toàn màn hình';
  fit();
});
new ResizeObserver(fit).observe(area);
window.visualViewport?.addEventListener('resize', fit);
canvas.addEventListener('contextmenu', event => event.preventDefault());
$('start').disabled = true;
try {
  const response = await fetch('web-settings.json', { cache: 'no-cache' });
  if (!response.ok) throw new Error(`Display settings: HTTP ${response.status}`);
  settings = await response.json();
  if (!Array.isArray(settings.modes) || !settings.modes.some(m => m.id === 'pc') ||
      settings.modes.some(m => !Number.isFinite(m.width) || !Number.isFinite(m.height) || m.width <= 0 || m.height <= 0)) {
    throw new Error('Invalid display settings');
  }
  for (const mode of settings.modes) {
    const button = document.createElement('button');
    button.className = 'ui-btn blue'; button.type = 'button';
    button.dataset.mode = mode.id; button.textContent = mode.label;
    button.addEventListener('click', () => selectMode(mode.id));
    $('modes').append(button);
  }
  let saved;
  try { saved = localStorage.getItem(storageKey); } catch { /* Use device default. */ }
  const touch = matchMedia('(pointer: coarse)').matches;
  const deviceDefault = touch ? (Math.min(innerWidth, innerHeight) >= 600 ? 'ipad' : 'phone') : 'pc';
  selectMode(saved || deviceDefault);
  $('start').disabled = false;
  $('start').addEventListener('click', start);
} catch (error) { showFailure(error); }
