# HTTP-proxy-server
##Необходимо реализовать простой прокси-сервер, выполняющий журналирование проксируемых HTTP-запросов.
Программа должна работать в виде службы и отображать в виде журнала краткую информацию о проксируемых запросах (URL и код ответа). При реализации использовать программный интерфейс сокетов.
Обязательной является поддержка HTTP, поддержка HTTPS не требуется.
Для проверки работоспособности необходимо настроить в браузере работу через прокси и загрузить любую страницу по HTTP (например, http://example.com/).
Дополнительное задание
Реализовать фильтрацию сайтов по черному списку. В конфигурационном файле для прокси-сервера задается список доменов и/или URL-адресов для блокировки. 
При попытке загрузить страницу из черного списка прокси-сервер должен вернуть предопределенную страницу с адресом, доступ к которому заблокирован, и сообщением об ошибке.