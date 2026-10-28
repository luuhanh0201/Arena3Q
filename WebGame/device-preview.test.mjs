import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import { deviceDisplay, devicePayload } from './device-preview.mjs';
const settings = JSON.parse(fs.readFileSync(new URL('./web-settings.json', import.meta.url)));
test('all phone ratios retain the phone mode and have valid buffer-independent safe areas', () => {
  for (const device of settings.phoneDevices) for (const preset of settings.displayPresets) {
    const display = deviceDisplay(settings, 'phone', device.id), payload = devicePayload(display, preset.frameRate);
    assert.equal(display.id, 'phone'); assert.equal(display.deviceId, device.id);
    assert.equal(payload.width, device.width); assert.equal(payload.height, device.height);
    assert.equal(payload.left, device.safeInsets.left/device.width);
    assert.equal(payload.frameRate, preset.frameRate);
  }
  assert.equal(deviceDisplay(settings, 'phone', 'missing').deviceId, settings.phoneDevices[0].id);
  assert.equal(deviceDisplay(settings, 'pc', 'redmi').deviceId, '');
});
test('invalid preview cannot send NaN or impossible insets to Unity', () => {
  for (const device of [{width:0,height:1080}, {width:1920,height:1080,safeInsets:{left:2000}},
    {width:1920,height:1080,safeInsets:{left:1500,right:500}}, {width:1920,height:1080,dpi:-1},
    {width:1920,height:1080,safeInsets:{left:NaN}}])
    assert.throws(() => devicePayload(device, 60));
});
