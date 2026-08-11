const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();

  page.on('console', (msg) => console.log(`[console:${msg.type()}] ${msg.text()}`));
  page.on('requestfailed', (req) => console.log(`[requestfailed] ${req.method()} ${req.url()} -- ${req.failure()?.errorText}`));
  page.on('response', (res) => {
    if (res.url().includes('hubs') || res.url().includes('negotiate')) {
      console.log(`[response] ${res.status()} ${res.url()}`);
    }
  });
  page.on('request', (req) => {
    if (req.url().includes('hubs') || req.url().includes('negotiate')) {
      console.log(`[request] ${req.method()} ${req.url()}`);
    }
  });

  await page.goto('http://localhost:5173/login', { waitUntil: 'networkidle' });
  await page.fill('#email', 'admin@aihelpdesk.com');
  await page.fill('#password', 'Admin@123');
  await page.click('button[type="submit"]');
  await page.waitForURL('**/dashboard', { timeout: 15000 });
  await page.waitForTimeout(3000);

  await browser.close();
})();
