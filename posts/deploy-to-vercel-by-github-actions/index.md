---
title: "Deploy to Vercel By GitHub Actions"
tags:
  - 技术笔记
create_time: 2022-02-23 23:45:00
---

## Prerequisites

Need an account of Vercel and Github, and three secrity keys:

- `VERCEL_TOKEN`
- `VERCEL_ORG_ID`
- `VERCEL_PROJECT_ID`

`VERCEL_TOKEN` can generate from Vercel tokens setting page: https://vercel.com/account/tokens.

Run this command on your website project directory to generate the other values:

```bash
npx vercel link
```

It will create a `.vercel` directory with a config file named `project.json`, and those values will be stored in this file.

## Config GitHub Actions

Add job steps like this:

```yml
jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:

    # Other steps, just like build, test or etc.

    - name: Deploy to vercel
      id: deploy-vercel-staging
      uses: amondnet/vercel-action@v20
      with:
        vercel-token: ${{ secrets.VERCEL_TOKEN }}
        vercel-org-id: ${{ secrets.VERCEL_ORG_ID }}
        vercel-project-id: ${{ secrets.VERCEL_PROJECT_ID }}
        vercel-args: '--prod' # if not set will deploy to Staging
        working-directory: ./dist # website directory
        scope: ${{ secrets.VERCEL_ORG_ID }}
```

A full example to see [here](https://github.com/tatwd/blog/blob/main/.github/workflows/ci.yml).
