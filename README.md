# fiitobot

[@fiitobot](https://t.me/fiitobot). Телеграм бот для ФИИТ УрФУ с контактами студентов.

## Развёртывание

1. Создайте Settings.Production.cs с реальными значениями всех настроек.
2. Проверьте, что в deploy.cmd указан правильный идентификатор сервисного аккаунта Yandex Cloud.
3. Запустите deploy.cmd − он создаст новую версию Cloud Function в Yandex Cloud.
4. Установите WebHook для телеграма командой:
```
curl --request POST --url https://api.telegram.org/<TOKEN>/setWebhook --header "content-type: application/json" --data "{""url"": ""https://functions.yandexcloud.net/<FUNCTIONID>""}"
```
