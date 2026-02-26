# Darshan Media Player README

## Overview

Darshan is a libre and open source media player and multimedia engine, tailored for Indian users and regional content while remaining fully cross‑platform.
It focuses on playing a wide variety of audio and video formats, handling local files, discs, devices, and network streams with an India‑first experience.

Darshan is built as a fork on top of the VLC media engine from the VideoLAN project, inheriting its mature core, codec support, and portability.
The playback engine can also be embedded into third‑party applications similarly to libVLC, allowing developers to integrate Darshan’s capabilities into their own projects.

## Key Goals

- Provide a great out‑of‑the‑box experience for Indian users, with sensible defaults for regional languages and content.
- Keep the project fully open source and community‑driven.
- Stay compatible with upstream improvements from the VLC engine wherever possible.

## License

Darshan is distributed under the GPLv2 (or later) license, as required by its upstream codebase.
On some platforms it may effectively behave as GPLv3 due to the licenses of bundled or required dependencies.

The embeddable playback engine used by Darshan is based on libVLC and therefore follows the LGPLv2 (or later) license for that component.
This permits integration into third‑party applications that may use other licenses, provided they respect the terms of the LGPL for the engine.

## Platforms

Darshan aims to support the same major desktop and mobile platforms as its upstream engine, subject to build and packaging status:

- Windows (7 and later, including modern Windows 10/11 variants)
- macOS (10.10 and later)
- GNU/Linux and related distributions
- BSD and related systems
- Android (4.2 and later), including Android TV form factors
- iOS (9 and later), including iPadOS and Apple TV devices

Exact platform support may vary over time depending on available maintainers and build infrastructure.
Mobile applications for Android and iOS are hosted in separate repositories from the core desktop engine, mirroring the upstream layout.

## India‑Focused Features

Darshan’s main differentiator is its focus on Indian users and media workflows:

- Better defaults for Indic subtitle fonts and languages.
- Quick access to folders and libraries commonly used for regional content.
- UX and theming that reflect Indian aesthetics without compromising usability.

These goals are implemented gradually on top of the stable upstream engine so that Darshan remains compatible with a wide range of existing media files and devices.

## Contributing and Community

Darshan is maintained by an open community of developers, designers, packagers, and documentation writers.
There is no corporate sponsorship; contributions are welcome from anyone who wants to improve the player for Indian and global users.

The core of Darshan continues to be written primarily in C, with additional pieces in C++, Objective‑C, assembly, and Rust, as inherited from the upstream engine.
Other companion projects or bindings may use languages such as Kotlin/Java for Android, Swift for iOS, and C# for .NET integrations.

Areas where contributions are especially helpful:

- Implementing and refining India‑specific UX features and preferences.
- Packaging for Windows, macOS, GNU/Linux distributions, and mobile app stores.
- Writing and updating technical documentation and tutorials.
- Visual design, iconography, and theming.
- User support, triaging issues, and community moderation.

## Contribution Workflow

The recommended workflow is to fork the repository, create a feature branch, and open a Merge Request or Pull Request against the main Darshan project.
All proposed changes should pass automated checks and receive review discussion before being merged.

For low‑level engine changes, contributors should stay aware of upstream VLC developments so that Darshan can periodically rebase or merge updates cleanly.
When relevant, improvements that are generally useful may also be proposed back to the VLC project through its own contribution channels.

## Engine Embedding (libDarshan)

Darshan exposes an embeddable engine, conceptually similar to libVLC, that can be used inside third‑party applications and frameworks.
The engine supports playback, streaming, and conversion of media files and network streams, making it suitable for richer media experiences.

Bindings can be created for multiple programming languages, including C++, Python, C#, and others, building on top of the underlying VLC‑derived engine APIs.
Developers are encouraged to contribute or maintain bindings that target popular Indian technology stacks and frameworks.

## Support and Resources

Darshan is still a community project, so support is mostly provided via public channels and documentation.
Typical resources may include:

- Project website and downloads page.
- Issue tracker for bug reports and feature requests.
- Community chat or forum for questions and discussion.
- Developer documentation and hacking guides inspired by upstream resources.

Because Darshan builds on VLC, upstream documentation, forums, and wikis from the VideoLAN project can remain a valuable reference for low‑level engine behavior and advanced configuration.

## Source Tree Overview

Darshan largely follows the same source tree structure as its upstream engine, with some project‑specific additions.
A typical layout includes:

- `bin/` – Darshan binaries and launchers.
- `bindings/` – Language bindings built on top of the embeddable engine.
- `compat/` – Compatibility helpers for operating systems that lack certain functionality.
- `contrib/` – Tools for fetching and building third‑party libraries needed by Darshan.
- `doc/` – Documentation and reference material.
- `extras/` – Auxiliary build, analysis, and packaging resources.
- `include/` – Public and internal header files.
- `lib/` – Source code for the embeddable playback engine.
- `modules/` – Plugins and modules providing codecs, formats, and interfaces.
- `po/` – Translation files for Darshan’s user interface.
- `share/` – Shared assets such as icons and UI resources.
- `src/` – Core engine code.
- `test/` – Test suites and harnesses used for continuous integration.

Individual files like `COPYING`, `COPYING.LIB`, `AUTHORS`, and `THANKS` remain important for licensing and attribution, and should be preserved from the upstream project with appropriate updates where necessary.
