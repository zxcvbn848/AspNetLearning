import assert from 'node:assert/strict'

// 在已啟動的 Development Docker Compose 環境執行：node tests/openapi-smoke.mjs。
const baseUrl = process.env.API_BASE_URL ?? 'http://localhost:8180'
const documentResponse = await fetch(`${baseUrl}/openapi/v1.json`)
assert.equal(documentResponse.status, 200)
const document = await documentResponse.json()

assert.equal(document.info.title, 'AspNetLearning API')
assert.ok(document.components.securitySchemes.Bearer)
assert.equal(document.components.securitySchemes.RefreshCookie.in, 'cookie')

const cases = [
  ['/auth/register', 'post', ['201', '400', '409'], null],
  ['/auth/login', 'post', ['200', '400', '401'], null],
  ['/auth/refresh', 'post', ['200', '401'], 'RefreshCookie'],
  ['/auth/logout', 'post', ['204'], null],
  ['/auth/me', 'get', ['200', '401'], 'Bearer'],
  ['/todos', 'get', ['200', '401'], 'Bearer'],
  ['/todos', 'post', ['201', '400', '401'], 'Bearer'],
  ['/todos/{id}', 'put', ['200', '400', '401', '404'], 'Bearer'],
  ['/todos/{id}', 'delete', ['204', '401', '404'], 'Bearer'],
]

for (const [path, method, statuses, security] of cases) {
  const operation = document.paths[path]?.[method]
  assert.ok(operation, `缺少 ${method.toUpperCase()} ${path}`)
  assert.deepEqual(Object.keys(operation.responses), statuses)
  assert.equal(operation.security?.[0] && Object.keys(operation.security[0])[0] || null, security)
}

assert.ok(document.paths['/hello/{name}'].get.responses['200'].content['application/json'])
assert.ok(document.paths['/debug/error'].get.responses['500'])
assert.ok(!Object.keys(document.components.schemas.UserResponse.properties).includes('passwordHash'))
assert.equal((await fetch(`${baseUrl}/scalar`)).status, 200)
console.log('PASS: OpenAPI 端點、狀態碼、授權需求與 Scalar 文件頁')
