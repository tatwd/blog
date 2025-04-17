[![Deploy blog](https://github.com/tatwd/blog/actions/workflows/ci.yml/badge.svg)](https://github.com/tatwd/blog/actions/workflows/ci.yml)

Dev (.net 9.0)

```sh
# Generate files to dist/
# or ./build.sh
# or ./dev.ps1 (Hot reload enabled)
dotnet run --project src/blog.csproj \
    --cwd ./ \
    --posts ./posts \
    --theme ./theme \
    --dist ./dist

# Preview by dotnet-serve or others
dotnet tool restore
dotnet serve --directory dist
```
