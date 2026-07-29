---
title: Replace the placeholder package icon
summary: Both logo files are generated placeholders with PLACEHOLDER on their face; the csproj and the README are already wired to them and need real artwork.
tags: [todo, packaging, nuget, branding]
created: 2026-07-28
priority: medium
effort: low
status: open
---

The logo is a placeholder: a slate tile with a dashed border reading
`IValue<T>` / `PLACEHOLDER` / `replace me`. It ships in the package, so it
renders on the nuget.org listing page and in the README. Replace it before
announcing the package anywhere.

There are two files, following plumber, which uses a separate image for each
role. pool uses one image for both.

| File | Size | Used by |
| --- | --- | --- |
| `images/valuetypes-logo.png` | 256x256 | the README header, over HTTP by raw GitHub URL |
| `images/valuetypes-logo.small.png` | 128x128 | the packed NuGet icon |

Both are referenced by path, so replacing the files in place is the only step.
For reference, those paths are:

- `src/ValueTypes/ValueTypes.csproj` — `<PackageIcon>valuetypes-logo.small.png</PackageIcon>`
  and the `None Include="..\..\images\valuetypes-logo.small.png"` pack item. Only
  the small file is packed, because the README's image resolves over HTTP.
- `README.md` — the raw GitHub URL above the MSL Armory mark.

Keep the sizes. 128x128 is NuGet's recommended icon size, and 256x256 matches
`pool.png`, `plumber.comic.small.png`, and `msl.armory.small.png`. The current
small file is a LANCZOS downscale of the 256. Real artwork should be exported at
each size rather than resampled.

Verify with `dotnet pack -c Release`, then confirm the icon landed:

```sh
unzip -l src/ValueTypes/bin/Release/MSL.ValueTypes.1.0.0.nupkg | grep png
```

Siblings keep an SVG source next to the PNG: pool has `pool.svg`, plumber has
`plumber.svg` and `plumber-ideas.svg`. value-type has no vector source, and
adding one would match them. result has the same gap, so whichever gets real
artwork first sets the pattern.

`images/msl.armory.small.png` is already correct, copied byte-identical from
result (md5 `a617544f38dac741bab7eb4b67df89c2`, the same file in pool and
plumber). Leave it alone.
