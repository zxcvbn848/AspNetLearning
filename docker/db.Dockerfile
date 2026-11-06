# 以固定上游原始碼與修補依賴重建 gosu，維持 PostgreSQL 18。
FROM golang:1.27.3 AS gosu-build
WORKDIR /src
RUN git init \
    && git remote add origin https://github.com/tianon/gosu.git \
    && git fetch --depth 1 origin 6456aaa0f3c854d199d0f037f068eb97515b7513 \
    && git checkout --detach FETCH_HEAD
RUN go get github.com/moby/sys/user@v0.4.1 golang.org/x/sys@v0.44.0 \
    && go test ./... \
    && CGO_ENABLED=0 go build -trimpath -o /out/gosu .

FROM postgres:18-alpine
RUN apk upgrade --no-cache brotli-libs libcurl
COPY --from=gosu-build /out/gosu /usr/local/bin/gosu
RUN gosu nobody true \
    && test "$(gosu postgres id -u)" = "$(id -u postgres)"
