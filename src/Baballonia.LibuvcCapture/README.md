# Baballonia LibuvcCapture Plugin

libuvc-based capture plugin for Bigscreen Beyond 2E eye tracking cameras on Linux.

## The Problem

The Bigscreen Beyond 2E eye tracking cameras (VID:PID = 35bd:0202) work on Windows but fail on Linux with V4L2. The device reports `Size Image: 0` for its MJPEG format, causing V4L2 buffer allocation to fail with `VIDIOC_REQBUFS: Invalid argument`.

This plugin bypasses V4L2 entirely by using libuvc to communicate directly with the USB device.

## Running

Install the .NET 10 SDK, libuvc, libusb-1.0, and libudev. The plugin uses the Linux runtime libraries `libuvc.so.0` and `libudev.so.1`; development packages are not required for these imports.

On Fedora:

```bash
sudo dnf install libuvc libusb1 systemd-libs
```

On Debian/Ubuntu:

```bash
sudo apt install libuvc0 libusb-1.0-0 libudev1
```

On Nix, `nix develop` from the repository root provides these dependencies. Launch the application from that shell so its library search path is preserved; the upstream flake no longer provides a `nix run` package.

Initialize the submodules, download the desktop dependencies, and build from the repository root:

```bash
git submodule update --init --recursive
./download_dependencies.sh
dotnet build src/Baballonia.Desktop/Baballonia.Desktop.csproj
```

The desktop build places the plugin in its output `Modules` directory. Run the application from the desktop build output.

## Device permissions

Libuvc opens the USB device directly, so membership in the `video` group alone is not enough. Do not run Baballonia with `sudo`.

On Fedora and other systemd desktop distributions, the included rule grants USB access to the active local desktop user through `uaccess`, without making the device world-writable. From the repository root, install it without replacing an existing rule:

```bash
sudo cp --no-clobber src/Baballonia.LibuvcCapture/70-baballonia-bigeye.rules /etc/udev/rules.d/
sudo udevadm control --reload-rules
```

Unplug and reconnect the headset's USB connection, then restart Baballonia as your normal user. Existing udev rules are left unchanged. The rule is intentionally named before systemd's seat rules so the ACL is applied.

This grants access to an active local desktop session, not a headless or SSH-only session. If access is still denied, check the ACL on the headset's `/dev/bus/usb/BBB/DDD` node (find the bus and device using `lsusb -d 35bd:0202`). It should include your user with read/write access.

## Usage

1. Launch Baballonia
2. The BSB2E device will appear in the camera dropdown as `/dev/video0` (or similar)
3. Select the device. If a capture backend selector is shown, choose `LibuvcCapture`.

Select the same Bigeye camera entry and backend in both eye panels. Baballonia shares one USB stream and displays the left half in the left-eye panel and the right half in the right-eye panel. Starting either eye alone also selects that eye's half. Other capture backends and your saved crop settings are unchanged; use the panel's whole-frame selection if an old crop no longer fits the single-eye image.

If it fails and the log contains errors about "no support for memory mapping" and "Insufficient buffer memory", then the wrong plugin was selected to handle the device.

If the log reports `DllNotFoundException`, check the native runtime libraries:

```bash
ldconfig -p | grep -E 'libuvc\.so\.0|libusb-1\.0\.so\.0|libudev\.so\.1'
```

Install any missing runtime packages, then restart Baballonia. On Nix, use the development shell instead of `ldconfig` to provide the library search path.

## License

This plugin is licensed under LGPL v3. See LICENSE file for details.

Incorporates code from libuvcnet (MIT License) - see LICENSE for attribution.

## Credits

Inspired by sample Go code by Lillia on the "Bigscreen Beyond on Linux" Discord server: https://discord.com/channels/1197445268220694661/1197445268220694663/1408943429718773831

P/Invoke bindings from https://github.com/horizongir/libuvcnet
