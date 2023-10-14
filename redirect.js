// Only used by Deno Deploy
// my blog target domain
const destDomain = "tatwd.deno.dev";

function handleRequest(request) {
  // const { pathname, host, searchParams } = new URL(request.url);
  // searchParams.set("__from", host);

  const url = new URL(request.url);
  url.host = destDomain;
  const targetUrl = url.toString();

  const html = `<!DOCTYPE html>
<html>
<head>
  <meta http-equiv="Refresh" content="0; URL=${targetUrl}" />
</head>
<body>
  Redirect to ${targetUrl} ...
</body>
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
