export function deviceDisplay(settings, modeId, deviceId) {
  const base = settings.modes.find(mode => mode.id === modeId) || settings.modes.find(mode => mode.id === 'pc');
  const device = base.id === 'phone' ? settings.phoneDevices?.find(item => item.id === deviceId) || settings.phoneDevices?.[0] : null;
  return { ...base, ...(device || {}), id: base.id, label: base.label, deviceLabel: device?.label || '', deviceId: device?.id || '' };
}

export function devicePayload(display, frameRate) {
  const insets = display.safeInsets || {};
  const result = { width: display.width, height: display.height, dpi: display.dpi ?? 0, frameRate,
    left: (insets.left ?? 0) / display.width, right: (insets.right ?? 0) / display.width,
    top: (insets.top ?? 0) / display.height, bottom: (insets.bottom ?? 0) / display.height };
  if (![result.width, result.height, result.frameRate].every(value => Number.isFinite(value) && value > 0)
      || !Number.isFinite(result.dpi) || result.dpi < 0
      || !['left', 'right', 'top', 'bottom'].every(key => Number.isFinite(result[key]) && result[key] >= 0 && result[key] < 1)
      || result.left + result.right >= 1 || result.top + result.bottom >= 1) throw new Error('Invalid device preview settings');
  return result;
}
