# ?? Гайд з публікації бета-версії сайту

## ?? Обрано: Azure App Service (БЕЗКОШТОВНИЙ рівень для тестування)

### ? Переваги Azure для вашого проекту:
- ? **Безкоштовний F1 план** (60 хв CPU/день)
- ?? **HTTPS з коробки**
- ???? **Підтримка українських доменів**
- ? **Швидке розгортання** (5-10 хв)
- ?? **Моніторинг вбудований**

---

## ?? Крок 1: Підготовка проекту

### 1.1 Перевірка збірки

```bash
# У терміналі (PowerShell):
cd "C:\Users\pvakb\source\repos\таблиця3\таблиця3"
dotnet build --configuration Release
```

### 1.2 Публікація проекту

```bash
# Створюємо папку для публікації
dotnet publish -c Release -o ./publish
```

Це створить папку `publish` з усіма файлами для розгортання.

---

## ?? Крок 2: Реєстрація в Azure

### 2.1 Створіть безкоштовний акаунт:
1. Відкрийте: https://azure.microsoft.com/uk-ua/free/
2. Натисніть **"Спробуйте безкоштовно"**
3. Увійдіть через Microsoft account
4. **Отримайте $200 кредитів** на 30 днів!

### 2.2 Встановіть Azure CLI (опціонально):
```bash
# Завантажте з:
https://aka.ms/installazurecliwindows
```

---

## ?? Крок 3: Створення Web App

### 3.1 Через портал Azure (найпростіше):

1. **Відкрийте портал**: https://portal.azure.com
2. Натисніть **"Створити ресурс"**
3. Виберіть **"Web App"**
4. Заповніть форму:

```
Назва програми: tablitsya3-beta
        (буде доступно як tablitsya3-beta.azurewebsites.net)

Підписка: Free Trial

Група ресурсів: [Створити нову] ? tablitsya3-rg

Среда выполнения:
  - Стек: .NET
  - Версія: .NET 9
  - ОС: Windows

Регіон: West Europe (найближче до України)

План: Free F1 (безкоштовно!)
```

5. Натисніть **"Перегляд + створення"** ? **"Створити"**

### 3.2 Через Azure CLI (швидший спосіб):

```bash
# 1. Увійдіть в Azure
az login

# 2. Створіть групу ресурсів
az group create --name tablitsya3-rg --location westeurope

# 3. Створіть план App Service (безкоштовний)
az appservice plan create --name tablitsya3-plan --resource-group tablitsya3-rg --sku F1

# 4. Створіть Web App
az webapp create --name tablitsya3-beta --resource-group tablitsya3-rg --plan tablitsya3-plan --runtime "DOTNET:9.0"
```

---

## ?? Крок 4: Розгортання сайту

### Метод 1: Через Visual Studio (найпростіше)

1. **У Visual Studio:**
   - Правий клік на проекті `таблиця3`
   - **Publish** (Опублікувати)
   - **Azure** ? **Next**
   - **Azure App Service (Windows)** ? **Next**
   - Виберіть `tablitsya3-beta` ? **Finish**
   - Натисніть **Publish**

2. **Очікуйте 2-5 хвилин**
3. Сайт автоматично відкриється! ??

### Метод 2: Через Azure CLI

```bash
# З папки проекту:
cd "C:\Users\pvakb\source\repos\таблиця3\таблиця3"

# Публікація
dotnet publish -c Release -o ./publish

# Створення ZIP архіву
Compress-Archive -Path ./publish/* -DestinationPath ./publish.zip -Force

# Розгортання
az webapp deploy --resource-group tablitsya3-rg --name tablitsya3-beta --src-path ./publish.zip --type zip
```

### Метод 3: Через FTP (якщо інше не працює)

```bash
# 1. Отримайте FTP credentials:
az webapp deployment list-publishing-credentials --name tablitsya3-beta --resource-group tablitsya3-rg

# 2. Використайте FileZilla або WinSCP для завантаження файлів з папки ./publish
```

---

## ?? Крок 5: Налаштування після розгортання

### 5.1 Налаштування Application Settings

У порталі Azure:
1. Відкрийте ваш Web App
2. **Configuration** ? **Application settings**
3. Додайте:

```
ASPNETCORE_ENVIRONMENT = Production
WEBSITE_HTTPLOGGING_RETENTION_DAYS = 7
```

### 5.2 Увімкніть логування

```bash
az webapp log config --name tablitsya3-beta --resource-group tablitsya3-rg --application-logging filesystem --level information
```

### 5.3 Перевірте сайт

Відкрийте: **https://tablitsya3-beta.azurewebsites.net**

---

## ?? Крок 6: Власний домен (опціонально)

### Якщо хочете свій домен (наприклад, production-planning.com.ua):

1. **Купіть домен** (рекомендую: https://www.ukraine.com.ua/)
2. У Azure Portal:
   - Відкрийте Web App
   - **Custom domains** ? **Add custom domain**
   - Введіть ваш домен
   - Додайте DNS записи (Azure покаже інструкції)

---

## ?? Крок 7: Безпека

### 7.1 Увімкніть HTTPS (автоматично ввімкнено)

### 7.2 Додайте базову автентифікацію (опціонально)

Створіть файл `таблиця3/Middleware/BasicAuthMiddleware.cs`:

```csharp
public class BasicAuthMiddleware
{
    private readonly RequestDelegate _next;
    private const string USERNAME = "beta";
    private const string PASSWORD = "test2025";

    public BasicAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.ContainsKey("Authorization"))
        {
    context.Response.StatusCode = 401;
context.Response.Headers.Append("WWW-Authenticate", "Basic realm=\"Beta Access\"");
    return;
        }

  var authHeader = context.Request.Headers["Authorization"].ToString();
        if (authHeader.StartsWith("Basic "))
        {
 var encodedCredentials = authHeader.Substring(6);
            var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
    var parts = credentials.Split(':');
    
            if (parts.Length == 2 && parts[0] == USERNAME && parts[1] == PASSWORD)
     {
        await _next(context);
return;
        }
        }

        context.Response.StatusCode = 401;
    }
}

// В Program.cs додайте:
// app.UseMiddleware<BasicAuthMiddleware>();
```

---

## ?? Крок 8: Моніторинг

### Перегляд логів у реальному часі:

```bash
# В PowerShell:
az webapp log tail --name tablitsya3-beta --resource-group tablitsya3-rg
```

### Або через портал:
1. Відкрийте Web App
2. **Monitoring** ? **Log stream**

---

## ?? Вирішення проблем

### Проблема: "Application Error"

```bash
# Перевірте логи:
az webapp log download --name tablitsya3-beta --resource-group tablitsya3-rg --log-file logs.zip

# Або увімкніть детальні помилки:
az webapp config set --name tablitsya3-beta --resource-group tablitsya3-rg --detailed-error-messages true
```

### Проблема: Дані не зберігаються

Azure File Storage перезаписується при кожному розгортанні. Для production використайте Azure Blob Storage або SQL Database.

**Швидке рішення для бета:**
```csharp
// В DataStorageService.cs змініть шлях:
private readonly string _filePath = Path.Combine(
    Environment.GetEnvironmentVariable("HOME") ?? ".", 
    "data", 
    "workshop-data.json"
);
```

---

## ?? Вартість

### Безкоштовний план F1:
- ? **$0/місяць**
- ?? 60 хв CPU/день
- ?? 1 GB RAM
- ?? 1 GB storage
- ? Достатньо для 5-10 користувачів одночасно

### Коли потрібен платний план:
- ?? **Basic B1** ($13/місяць) - 100 користувачів
- ?? **Standard S1** ($70/місяць) - 1000 користувачів

---

## ?? Чеклист перед запуском

- [ ] Проект компілюється без помилок
- [ ] Тестування на localhost працює
- [ ] Створено Azure акаунт
- [ ] Створено Web App в Azure
- [ ] Розгорнуто код на Azure
- [ ] Сайт відкривається в браузері
- [ ] Діаграми Ганта відображаються коректно
- [ ] Дані зберігаються і завантажуються
- [ ] Налаштовано логування

---

## ?? Готово!

Ваш сайт буде доступний за адресою:
**https://tablitsya3-beta.azurewebsites.net**

Поділіться цією адресою з бета-тестерами! ??

---

## ?? Підтримка

**Проблеми з Azure?**
- Документація: https://learn.microsoft.com/azure/app-service/
- Форум: https://stackoverflow.com/questions/tagged/azure-web-app-service

**Питання з Blazor?**
- Документація: https://learn.microsoft.com/aspnet/core/blazor/
- GitHub: https://github.com/dotnet/aspnetcore

---

**Версія гайду:** 1.0  
**Дата:** Листопад 2024  
**Автор:** GitHub Copilot  
