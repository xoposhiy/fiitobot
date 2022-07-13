rmdir /S /Q fiitobot3\bin
rmdir /S /Q fiitobot3\obj
yc serverless function version create --function-name fiitobot-handler --entrypoint fiitobot.TelegramBotHandlerFunction --runtime dotnetcore31 --service-account-id ajet10dfncm9rjmfdets --execution-timeout 60s --source-path fiitobot3