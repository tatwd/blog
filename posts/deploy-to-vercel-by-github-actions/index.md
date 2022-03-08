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


