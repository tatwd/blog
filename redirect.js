// Only used by Deno Deploy
const destWebsite = "https://blog.cloong.me";

function handleRequest(request) {
  const { pathname, search } = new URL(request.url);
  const html = `<!DOCTYPE html>
<html>
<head>
  <meta http-equiv="Refresh" content="0; URL=${destWebsite}${pathname}${search}" />
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
