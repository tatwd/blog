[![Deploy blog](https://github.com/tatwd/blog/actions/workflows/ci.yml/badge.svg)](https://github.com/tatwd/blog/actions/workflows/ci.yml)

Dev (.net 6.0)

```sh
# Generate files to dist/
# or ./build.sh
dotnet run --project src/blog.csproj \
    --cwd ./ \
    --posts ./posts \
    --theme ./theme \
    --dist ./dist

# Preview by dotnet-serve or others
dotnet tool restore
dotnet serve --directory dist
```
