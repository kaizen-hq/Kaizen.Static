# Kaizen.Static

Three small .NET 11 libraries that turn a **Blazor SSR** app into a flat static site (for example on Cloudflare Pages):

| Package | Assembly / namespace | What it does |
|--|--|--|
| `KaizenIO.StaticSite` | `Kaizen.StaticSite` | Exporter: endpoint inventory, `export` / `check` / `routes` commands, `route/index.html`, `404.html`, `sitemap.xml` (honest `lastmod`), `robots.txt`, `kaizen-manifest.json`, static assets, `_headers` / `_redirects` passthrough with Cloudflare limit warnings, internal link checking |
| `KaizenIO.Seo` | `Kaizen.Seo` | `<SeoHead/>` (title, description, canonical, Open Graph, Twitter, `article:*` dates) and JSON-LD helpers (`WebPage`, `BlogPosting`, `BreadcrumbList`, `Organization`) |
| `KaizenIO.BlogEngine` | `Kaizen.BlogEngine` | Markdown content collections (Markdig + YamlDotNet): strict YAML front matter (`title`, `date` required; `updated`, `description`, `author`, `slug`, `tags`, `draft`, `layout`), `IContentCollection`, `ContentCatalog.FindByUrl` / `CollectionOf` |

The libraries do **not** reference each other; the site wires them together. The libraries are trim/AOT-annotated (`IsAotCompatible`):
JSON-LD and the export manifest use System.Text.Json source generation, and front matter uses YamlDotNet's source-generated
`StaticDeserializerBuilder`. (ASP.NET Core Razor Components themselves do not support trimming or native AOT yet.) Package IDs use the `KaizenIO.` prefix because `Kaizen.*` is reserved on nuget.org by another account.

Extracted with history from [kijanawoodard/nucleics](https://github.com/kijanawoodard/nucleics) (the reference site). Issue/PR numbers like `(#10)` in older commit messages refer to that repo.

## Install

```sh
dotnet add package KaizenIO.StaticSite
dotnet add package KaizenIO.Seo
dotnet add package KaizenIO.BlogEngine
```

## Usage

```csharp
using Kaizen.BlogEngine;
using Kaizen.Seo;
using Kaizen.StaticSite;

const string SiteUrl = "https://example.org";
var builder = WebApplication.CreateBuilder(args);

builder.AddStaticSite(args, o => o.SiteUrl = SiteUrl);                                         // exporter
builder.Services.AddContentCollection("pages", "content/pages", urlPrefix: "");                // /{slug}/
builder.Services.AddContentCollection("blog", "content/posts", "/blog", c => { c.Index = true; c.Tags = true; });
builder.Services.AddSeo(o => { o.SiteUrl = SiteUrl; o.SiteName = "Example"; });
// site glue: adapt each collection to IStaticRouteSource (+ lastmod) and IStaticPageGate; see nucleics Glue/CollectionRoutes.cs
builder.Services.AddRazorComponents();

var app = builder.Build();
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.MapStaticAssets();
app.MapRazorComponents<App>();
return await app.RunStaticSiteAsync(args);   // `-- export`, `-- check`, `-- routes`; anything else runs the normal server
```

```sh
dotnet run --project src/MySite -- export   # write ./output
dotnet run --project src/MySite -- check    # CI gate: no files written, exit 1 on any problem
```

`export` boots the app on a loopback port, fetches every route, and fails (non-zero exit, stale `output/` removed) on a non-200 route,
a missing 404 probe, an empty inventory or a reference to a dropped asset. Blazor framework scripts (`_framework/*`) are not exported unless
`--keep-framework` is passed. Parameterised routes are exported only through an `IStaticRouteSource`.

## Build

Requires exactly the .NET SDK pinned in `global.json` (11.0.100, `rollForward: disable`; CI's `actions/setup-dotnet` installs that exact
version from `global.json`). Bump `global.json` deliberately to move to a newer SDK. Versions are managed centrally in `Directory.Packages.props`.

```sh
dotnet build Kaizen.Static.slnx
dotnet test Kaizen.Static.slnx
dotnet pack Kaizen.Static.slnx -c Release -o artifacts   # KaizenIO.*.nupkg + .snupkg (SourceLink)
```

## Releasing

Push a tag `vX.Y.Z`. `.github/workflows/publish.yml` builds, tests, packs all three packages with that version and pushes them to nuget.org
with [Trusted Publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing) (OIDC via `NuGet/login`; no stored API key).
It needs the repository variable `NUGET_USER` (the nuget.org account that owns the Trusted Publishing policy).

## License

MIT, Copyright (c) 2026 Kaizenio, Inc.
