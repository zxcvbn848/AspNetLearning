FROM node:22-alpine

# shortcut: npm 11.21.1 仍有 8 筆掃描項目，待上游修補後重新掃描並更新。
RUN npm install --global npm@11.21.1
