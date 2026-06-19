# Lait.Umbraco.GoToContent

> One click from the live page to the content editor.

**Go To Content** drops a floating **"Edit in Umbraco"** pill in the top-right corner of your public-facing pages. It is shown **only** to editors who are already logged in to the Umbraco backoffice — regular visitors never see it. Clicking it opens the content editor for the exact node you're looking at, in a new tab.

No more copy-pasting node IDs or hunting through the content tree. See something to fix on the live site → click → you're editing it.

![The "Edit in Umbraco" pill on a public page](https://raw.githubusercontent.com/laitdevelopers/Lait.Umbraco.GoToContent/master/docs/screenshot.png)

## Install

```bash
dotnet add package Lait.Umbraco.GoToContent
```

That's it. The package self-registers through an `IComposer` — **zero configuration**. Build, run, and browse your site while signed in to `/umbraco`.

## How it works

- A lightweight middleware runs late in the Umbraco pipeline and inspects each HTML response.
- If — and only if — the current request belongs to an authenticated **backoffice** user, it injects a small inline `<style>` + `<script>` + JSON config block just before `</body>`.
- The script renders the pill and points it at the backoffice deep-link for the current published node (`/umbraco/section/content/workspace/document/edit/{key}`).
- Anonymous visitors get a byte-for-byte unmodified response — nothing is injected, nothing leaks.

The injected markup is self-contained (no external requests), respects `prefers-color-scheme`, hides itself in print, and is accessible (focusable, `aria-label`).

## Compatibility

| Package | Target frameworks | Umbraco | .NET |
|---------|-------------------|---------|------|
| `Lait.Umbraco.GoToContent` 1.0.x | `net9.0`, `net10.0` | **15 – 17** | 9.0 / 10.0 |

The `net9.0` assembly serves Umbraco 15 and 16; the `net10.0` assembly serves Umbraco 17. The upper bound is open — newer Umbraco majors on .NET 9/10 are expected to work and will be added to this matrix as they're verified.

> **Need Umbraco 14?** Umbraco 14 runs on .NET 8, which can't load a `net9.0` assembly. Open an issue if you need a `net8.0` build.

## Configuration

None required. The pill text is "Edit in Umbraco" and it is positioned top-right by default.

## License

[MIT](LICENSE.md) © Lait
