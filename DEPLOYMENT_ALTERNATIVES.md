# ?? Альтернативні варіанти хостингу

## Якщо Azure не підходить, є інші варіанти:

---

## 1?? **Railway.app** (Найпростіший!)

### ? Переваги:
- ?? **$5 безкоштовних кредитів/місяць**
- ? **Дуже швидке розгортання** (2-3 хв)
- ?? **Автоматичне розгортання з GitHub**
- ?? **Безкоштовний домен** (.railway.app)

### ?? Інструкція:

1. **Відкрийте**: https://railway.app
2. **Sign up з GitHub**
3. **New Project** ? **Deploy from GitHub repo**
4. Виберіть ваш репозиторій
5. Railway автоматично визначить .NET проект
6. **Deploy** ? Готово! ??

**Результат**: https://tablitsya3.up.railway.app

---

## 2?? **DigitalOcean App Platform**

### ? Переваги:
- ?? **$200 кредитів** на 60 днів для нових користувачів
- ?? **Потужніше за Azure F1**
- ?? **Дата-центри в Європі**

### ?? Вартість:
- **Basic**: $5/місяць
- **Professional**: $12/місяць

### ?? Інструкція:

```bash
# 1. Встановіть doctl CLI
# Завантажте з: https://github.com/digitalocean/doctl/releases

# 2. Авторизуйтесь
doctl auth init

# 3. Створіть додаток
doctl apps create --spec app.yaml
```

Створіть файл `app.yaml`:
```yaml
name: tablitsya3-beta
region: fra
services:
  - name: web
    source_dir: /таблиця3
    github:
      repo: ваш-username/таблиця3
      branch: main
    build_command: dotnet publish -c Release -o ./publish
    run_command: dotnet ./publish/таблиця3.dll
    environment_slug: dotnet
    http_port: 5000
    instance_count: 1
    instance_size_slug: basic-xxs
```

---

## 3?? **Heroku** (з Docker)

### ? Переваги:
- ?? **Підтримка Docker**
- ?? **Автоматичне розгортання**
- ?? **Вбудований моніторинг**

### ?? Вартість:
- **Eco Dynos**: $5/місяць
- **Basic**: $7/місяць

### ?? Інструкція:

1. Створіть `Dockerfile`:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY таблиця3/*.csproj ./
RUN dotnet restore

COPY таблиця3/. ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .

ENV ASPNETCORE_URLS=http://+:$PORT
CMD ["dotnet", "таблиця3.dll"]
```

2. Розгорніть:
```bash
# Встановіть Heroku CLI
# https://devcenter.heroku.com/articles/heroku-cli

heroku login
heroku create tablitsya3-beta
heroku container:push web -a tablitsya3-beta
heroku container:release web -a tablitsya3-beta
```

---

## 4?? **Fly.io** (Швидкий і дешевий)

### ? Переваги:
- ?? **Безкоштовний план** (до 3 VM)
- ? **Дуже швидкий**
- ?? **Глобальна мережа**

### ?? Інструкція:

```bash
# 1. Встановіть flyctl
# https://fly.io/docs/hands-on/install-flyctl/

# 2. Авторизуйтесь
fly auth signup

# 3. Створіть додаток
fly launch --name tablitsya3-beta

# 4. Розгорніть
fly deploy
```

Fly автоматично створить `fly.toml`.

---

## 5?? **Власний VPS** (Найдешевше для довготривалого використання)

### ?? Провайдери:

#### ???? **Ukraine.com.ua** (підтримуємо своїх!)
- **Базовий VPS**: ?250/місяць (~$6)
- 2 CPU, 2 GB RAM, 40 GB SSD
- **Дата-центр в Україні**

#### ?? **Hetzner** (Німеччина - близько до України)
- **CX11**: €4.49/місяць (~$5)
- 1 vCPU, 2 GB RAM, 20 GB SSD
- **Дуже швидкий інтернет**

#### ?? **Vultr**
- **Regular**: $6/місяць
- 1 CPU, 1 GB RAM, 25 GB SSD

### ?? Налаштування VPS:

```bash
# 1. Підключіться по SSH
ssh root@your-server-ip

# 2. Встановіть .NET 9
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 9.0

# 3. Встановіть Nginx
apt update
apt install nginx

# 4. Налаштуйте reverse proxy
nano /etc/nginx/sites-available/tablitsya3

# Вставте:
server {
    listen 80;
  server_name your-domain.com;
    
    location / {
  proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
    }
}

# 5. Активуйте конфіг
ln -s /etc/nginx/sites-available/tablitsya3 /etc/nginx/sites-enabled/
systemctl restart nginx

# 6. Завантажте ваш додаток
scp -r ./publish/* root@your-server-ip:/var/www/tablitsya3/

# 7. Створіть systemd service
nano /etc/systemd/system/tablitsya3.service

# Вставте:
[Unit]
Description=Tablitsya3 Production Planning

[Service]
WorkingDirectory=/var/www/tablitsya3
ExecStart=/root/.dotnet/dotnet /var/www/tablitsya3/таблиця3.dll
Restart=always
RestartSec=10
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000

[Install]
WantedBy=multi-user.target

# 8. Запустіть сервіс
systemctl enable tablitsya3
systemctl start tablitsya3

# 9. Налаштуйте SSL (безкоштовно!)
apt install certbot python3-certbot-nginx
certbot --nginx -d your-domain.com
```

---

## ?? Порівняльна таблиця

| Хостинг | Ціна/міс | Складність | Швидкість | Рекомендую для |
|---------|----------|------------|-----------|----------------|
| **Azure** | $0 (F1) | ?? | ??? | .NET проектів, початківців |
| **Railway** | ~$5 | ? | ???? | Швидкого старту |
| **DigitalOcean** | $5-12 | ??? | ???? | Професійного використання |
| **Heroku** | $5-7 | ?? | ??? | Dockerized проектів |
| **Fly.io** | $0-5 | ?? | ????? | Глобального розгортання |
| **VPS** | $5-10 | ????? | ???? | Повного контролю |

---

## ?? Моя рекомендація для вашого проекту:

### ?? **Для бета-тестування: Railway.app**
- ? Найшвидше (2-3 хв)
- ? Найпростіше
- ? Безкоштовно для початку

### ?? **Для production: Azure App Service**
- ? Найкраща інтеграція з .NET
- ? Професійний моніторинг
- ? Масштабується

### ?? **Для довготривалого production: Власний VPS**
- ? Найдешевше ($5/міс)
- ? Повний контроль
- ? Необмежена кількість користувачів

---

## ?? Швидкий старт для Railway (РЕКОМЕНДУЮ!)

```bash
# 1. Встановіть Railway CLI
npm i -g @railway/cli

# Або завантажте: https://railway.app/cli

# 2. Авторизуйтесь
railway login

# 3. Створіть проект
railway init

# 4. Розгорніть
railway up

# Готово! Railway покаже URL вашого сайту
```

**Час розгортання: 2-3 хвилини! ?**

---

## ?? Потрібна допомога?

Напишіть який варіант обрали, і я допоможу з налаштуванням! ??
