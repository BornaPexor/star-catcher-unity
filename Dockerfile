FROM nginx:stable-alpine@sha256:dc5069ad14f19660b141b21236140b91656bf89bbc3e2417c70ae650cd66104c
COPY deploy/nginx.conf /etc/nginx/conf.d/default.conf
COPY Web/ /usr/share/nginx/html/
EXPOSE 80
