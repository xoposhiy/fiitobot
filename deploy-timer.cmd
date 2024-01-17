rmdir /S /Q fiitobot3\bin
yc serverless function version create --function-name fiitobot-timer --entrypoint fiitobot.TelegramBotTimerFunction --runtime dotnetcore31 --service-account-id ajejkko27gn149fp36q9 --execution-timeout 120s --source-path fiitobot3
