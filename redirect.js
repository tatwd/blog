// Only used by Deno Deploy
// my blog target domain
const destWebsite = "https://tatwd-blog.vercel.app";

function handleRequest(request) {
  const { pathname, host, searchParams } = new URL(request.url);
  // searchParams.set("__from", host);

  const html = `<!DOCTYPE html>
<html>
<head>
  <meta http-equiv="Refresh" content="0; URL=${destWebsite}${pathname}?${searchParams.toString()}" />
</head>
</html>`;
  return new Response(html, {
    headers: {
      "content-type": "text/html; charset=UTF-8",
    },
  });
}

addEventListener("fetch", (event) => {
  event.respondWith(handleRequest(event.request));
});
