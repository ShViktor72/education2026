# Практическая работа: настройка DHCP-сервера в Linux

**Темы:** VirtualBox, статический адрес сервера, ISC DHCP Server, DHCP pool, lease, options, reservation, журналы, `tcpdump`, диагностика DHCP  
**Операционная система сервера:** Debian Server 12/13 или Ubuntu Server 22.04/24.04 LTS  
**Операционная система клиента:** Debian/Ubuntu или Windows 10/11  
**Формат работы:** самостоятельная практическая работа  
**Ориентировочное время:** 4–6 академических часов

---

## 1. Что настроим

В этой работе соберём небольшой DHCP-стенд в VirtualBox.

На сервере `dhcp01`:

- назначим статический IP-адрес внутреннему интерфейсу;
- установим ISC DHCP Server;
- укажем интерфейс, на котором сервер принимает запросы;
- создадим диапазон динамических адресов;
- настроим время аренды;
- передадим клиенту маску, gateway, DNS и доменное имя;
- закрепим постоянный адрес за клиентом по MAC-адресу;
- проверим lease-файл, журналы и обмен DORA;
- разберём типовые неисправности по понятной последовательности.

На клиенте:

- включим автоматическое получение сетевых параметров;
- запросим новую аренду;
- проверим IP-адрес, маршрут, DNS и срок аренды;
- убедимся, что reservation работает.

Главная идея работы:

> DHCP считается настроенным не тогда, когда служба просто запустилась, а тогда, когда клиент получил правильный адрес и параметры, а на сервере появились lease и записи DORA.

---

## 2. Схема учебного стенда

Минимальный вариант состоит из двух виртуальных машин.

| Виртуальная машина | Назначение | Сетевые адаптеры | Адрес |
|---|---|---|---|
| `dhcp01` | DHCP-сервер | NAT + внутренняя сеть | `192.168.10.10/24` на внутреннем интерфейсе |
| `client01` | DHCP-клиент | внутренняя сеть | получает адрес автоматически |

Дополнительно можно подключить:

| Виртуальная машина | Назначение | Адрес |
|---|---|---|
| `client02` | проверка нескольких аренд | получает адрес автоматически |
| `client-win` | проверка с Windows | получает адрес автоматически |

Логическая схема:

```text
Интернет
   │
VirtualBox NAT
   │
 enp0s3
┌─────────────────────────────┐
│ dhcp01                      │
│ Debian/Ubuntu Server        │
│ enp0s8: 192.168.10.10/24    │
│ ISC DHCP Server, UDP/67     │
└──────────────┬──────────────┘
               │ внутренняя сеть VirtualBox: dhcp-lab
        ┌──────┴──────┐
        │             │
   client01       client02
   DHCP-клиент    DHCP-клиент
```

У сервера два адаптера:

- NAT нужен для установки пакетов;
- внутренняя сеть нужна для выдачи адресов клиентам.

У клиента достаточно одного адаптера, подключённого к внутренней сети.

---

## 3. Почему используем внутреннюю сеть VirtualBox

Для этой работы удобнее выбрать тип подключения **«Внутренняя сеть»**, а не «NAT Network» и не «Сетевой мост».

Причина простая: в учебном сегменте должен работать только наш DHCP-сервер. Если оставить рядом другой DHCP-сервис, клиент может получить адрес не от `dhcp01`, и диагностика станет запутанной.

Внутренняя сеть VirtualBox:

- изолирует лабораторный трафик от реальной локальной сети;
- не запускает собственный DHCP-сервер;
- позволяет спокойно смотреть широковещательные DHCP-пакеты;
- снижает риск случайно выдать адрес реальным устройствам.

> Сетевой мост для этой работы не используем. Неправильно настроенный DHCP-сервер в режиме моста может начать отвечать устройствам реальной сети.

---

## 4. Подготовка виртуальных машин в VirtualBox

### 4.1. Параметры сервера `dhcp01`

Рекомендуемая конфигурация:

| Параметр | Значение |
|---|---|
| Процессор | 2 виртуальных ядра |
| Оперативная память | 2 ГБ |
| Системный диск | 20 ГБ |
| Адаптер 1 | NAT |
| Адаптер 2 | внутренняя сеть `dhcp-lab` |

В настройках VirtualBox открываем:

```text
Настроить → Сеть
```

Для первого адаптера выбираем:

```text
Включить сетевой адаптер: да
Тип подключения: NAT
```

Для второго адаптера выбираем:

```text
Включить сетевой адаптер: да
Тип подключения: Внутренняя сеть
Имя: dhcp-lab
```

### 4.2. Параметры клиента `client01`

Рекомендуемая конфигурация:

| Параметр | Значение |
|---|---|
| Процессор | 1–2 виртуальных ядра |
| Оперативная память | 1–2 ГБ |
| Системный диск | 15–20 ГБ |
| Адаптер 1 | внутренняя сеть `dhcp-lab` |

Имя внутренней сети должно совпадать точно:

```text
dhcp-lab
```

`dhcp-lab`, `DHCP-LAB` и `dhcp_lab` — разные имена.

### 4.3. Создание снимка

Перед настройкой сервера создаём снимок обеих виртуальных машин:

```text
Снимки → Создать → До настройки DHCP
```

Снимок поможет быстро вернуться к исходному состоянию, если конфигурационный файл будет сильно изменён.

### Способ проверки

Открываем настройки обеих машин и убеждаемся, что:

- сервер имеет два адаптера;
- клиент имеет адаптер во внутренней сети;
- имя внутренней сети у обеих машин одинаковое;
- клиент не подключён сетевым мостом к реальной сети.

---

## 5. Адресный план лаборатории

Используем сеть:

```text
192.168.10.0/24
```

План адресов:

| Назначение | Адрес или диапазон |
|---|---|
| Адрес сети | `192.168.10.0` |
| DHCP-сервер | `192.168.10.10` |
| Учебный gateway | `192.168.10.10` |
| Reservation для `client01` | `192.168.10.50` |
| Динамический pool | `192.168.10.100–192.168.10.150` |
| Broadcast | `192.168.10.255` |
| Маска | `255.255.255.0` |

Адрес reservation находится вне динамического pool. Так снижается риск конфликта между закреплённым и случайно выданным адресом.

В этой работе адрес `192.168.10.10` передаётся клиенту как gateway, потому что это адрес сервера. Само назначение адреса через DHCP будет работать и без настройки маршрутизации. Доступ в интернет не является обязательным результатом этой работы.

---

## 6. Первичная проверка сервера

Входим в консоль `dhcp01`.

### 6.1. Проверяем имя системы

```bash
hostnamectl
```

Команда показывает имя компьютера, операционную систему и версию ядра.

Пример ожидаемого вывода:

```text
 Static hostname: ubuntu-server
       Icon name: computer-vm
         Chassis: vm
      Machine ID: ...
 Operating System: Ubuntu 24.04 LTS
           Kernel: Linux 6.8.0-xx-generic
     Architecture: x86-64
```

Задаём понятное имя серверу:

```bash
sudo hostnamectl set-hostname dhcp01
```

Проверяем результат:

```bash
hostnamectl --static
```

Ожидаемый вывод:

```text
dhcp01
```

### 6.2. Проверяем сетевые интерфейсы

```bash
ip -br link
```

Команда показывает интерфейсы в коротком формате.

Пример ожидаемого вывода:

```text
lo               UNKNOWN        00:00:00:00:00:00
 enp0s3           UP             08:00:27:11:22:33
 enp0s8           UP             08:00:27:44:55:66
```

Обычно:

- `enp0s3` — первый адаптер NAT;
- `enp0s8` — второй адаптер внутренней сети.

Имена могут отличаться. Дальше в примерах используется `enp0s8`, но в своей системе подставляем фактическое имя внутреннего интерфейса.

Смотрим текущие адреса:

```bash
ip -br address
```

Пример вывода до настройки:

```text
lo               UNKNOWN        127.0.0.1/8 ::1/128
 enp0s3           UP             10.0.2.15/24
 enp0s8           UP
```

Это нормальная картина:

- NAT-интерфейс получил адрес от VirtualBox;
- внутренний интерфейс пока не имеет IPv4-адреса.

### Способ проверки

Нужно уверенно определить, какой интерфейс подключён к `dhcp-lab`. Для дополнительной проверки можно временно отключить кабель второго адаптера в VirtualBox и снова выполнить:

```bash
ip -br link
```

У нужного интерфейса состояние изменится на `DOWN` или `NO-CARRIER`. После проверки кабель снова включаем.

---

## 7. Настройка статического адреса сервера

DHCP-сервер не должен получать свой основной адрес динамически. Клиенты должны всегда знать, где находится сервер, а служба должна быть привязана к стабильной подсети.

Настраиваем на внутреннем интерфейсе:

```text
192.168.10.10/24
```

### 7.1. Ubuntu Server: Netplan

Сначала смотрим имя файла Netplan:

```bash
ls -l /etc/netplan
```

Пример ожидаемого вывода:

```text
-rw------- 1 root root 245 Jul 16 10:20 50-cloud-init.yaml
```

Имя может быть другим, например:

```text
00-installer-config.yaml
```

Открываем найденный файл:

```bash
sudo nano /etc/netplan/50-cloud-init.yaml
```

Пример конфигурации:

```yaml
network:
  version: 2
  ethernets:
    enp0s3:
      dhcp4: true
    enp0s8:
      dhcp4: false
      addresses:
        - 192.168.10.10/24
```

В YAML важны отступы. Используем пробелы, а не табуляцию.

Проверяем конфигурацию безопасным способом:

```bash
sudo netplan try
```

Команда временно применяет настройки. Если всё работает, подтверждаем их в консоли.

Затем применяем конфигурацию окончательно:

```bash
sudo netplan apply
```

Проверяем адрес:

```bash
ip -br address show enp0s8
```

Ожидаемый вывод:

```text
enp0s8           UP             192.168.10.10/24
```

### 7.2. Debian Server: `/etc/network/interfaces`

Сначала смотрим текущий файл:

```bash
cat /etc/network/interfaces
```

Открываем его для редактирования:

```bash
sudo nano /etc/network/interfaces
```

Пример конфигурации:

```text
auto lo
iface lo inet loopback

auto enp0s3
iface enp0s3 inet dhcp

auto enp0s8
iface enp0s8 inet static
    address 192.168.10.10/24
```

Перезапускаем сетевую службу:

```bash
sudo systemctl restart networking
```

Проверяем адрес:

```bash
ip -br address show enp0s8
```

Ожидаемый вывод:

```text
enp0s8           UP             192.168.10.10/24
```

Если служба не перезапустилась, смотрим причину:

```bash
sudo systemctl status networking --no-pager
```

и:

```bash
sudo journalctl -u networking -n 30 --no-pager
```

### Способ проверки

На сервере выполняем:

```bash
ip address show enp0s8
```

В выводе должна быть строка:

```text
inet 192.168.10.10/24 brd 192.168.10.255 scope global enp0s8
```

Проверяем таблицу маршрутов:

```bash
ip route
```

Ожидаем увидеть подключённую сеть:

```text
192.168.10.0/24 dev enp0s8 proto kernel scope link src 192.168.10.10
```

---

## 8. Подготовка DHCP-клиента

На `client01` настраиваем автоматическое получение IPv4-параметров.

Предполагается, что у клиента один интерфейс, например `enp0s3`, подключённый к внутренней сети `dhcp-lab`.

Смотрим имя интерфейса:

```bash
ip -br link
```

Пример ожидаемого вывода:

```text
lo               UNKNOWN        00:00:00:00:00:00
 enp0s3           UP             08:00:27:aa:bb:cc
```

Запоминаем MAC-адрес. Он понадобится для reservation.

### 8.1. Ubuntu-клиент: Netplan

Сначала смотрим имя файла Netplan:

```bash
ls /etc/netplan
```

Открываем найденный файл:

```bash
sudo nano /etc/netplan/50-cloud-init.yaml
```

Пример конфигурации:

```yaml
network:
  version: 2
  ethernets:
    enp0s3:
      dhcp4: true
```

Применяем:

```bash
sudo netplan apply
```

Пока DHCP-сервер не настроен, интерфейс может остаться без IPv4-адреса. Это ожидаемо.

### 8.2. Debian-клиент: `/etc/network/interfaces`

Открываем файл:

```bash
sudo nano /etc/network/interfaces
```

Пример конфигурации:

```text
auto lo
iface lo inet loopback

auto enp0s3
iface enp0s3 inet dhcp
```

Применяем:

```bash
sudo systemctl restart networking
```

Пока сервер не выдаёт адреса, служба может дольше запускать интерфейс. Это тоже ожидаемо.

### 8.3. Проверяем состояние до запуска DHCP

```bash
ip -br address show enp0s3
```

Возможный вывод:

```text
enp0s3           UP
```

или адрес автоматической конфигурации:

```text
enp0s3           UP             169.254.15.23/16
```

Адрес `169.254.0.0/16` не выдан нашим DHCP-сервером. Он означает, что нормальный IPv4-адрес пока не получен.

---

## 9. Проверка связи на канальном уровне

До установки DHCP убеждаемся, что сервер и клиент находятся в одном виртуальном сегменте.

На сервере смотрим интерфейс:

```bash
ip link show enp0s8
```

Ожидаем состояние:

```text
state UP
```

На клиенте смотрим свой интерфейс:

```bash
ip link show enp0s3
```

Ожидаем то же состояние:

```text
state UP
```

Если клиент пока не имеет IPv4-адреса, обычный `ping` использовать рано. Для DHCP достаточно, чтобы оба интерфейса были включены и находились в одной внутренней сети VirtualBox.

### Способ проверки

Сверяем:

- имя внутренней сети VirtualBox;
- включённый флажок «Кабель подключён»;
- состояние `UP` у обоих интерфейсов;
- отсутствие второго DHCP-сервера в этом сегменте.

---

## 10. Установка ISC DHCP Server

На `dhcp01` обновляем список пакетов:

```bash
sudo apt update
```

Устанавливаем DHCP-сервер и `tcpdump`:

```bash
sudo apt install -y isc-dhcp-server tcpdump
```

### Что устанавливается

- `isc-dhcp-server` — служба DHCP для IPv4 и IPv6;
- `tcpdump` — анализатор пакетов для проверки DORA;
- `dhcpd` — серверный процесс, который слушает UDP-порт 67.

Во время установки служба может показать ошибку запуска. Это нормально: интерфейс и подсеть ещё не указаны.

Проверяем наличие программы:

```bash
command -v dhcpd
```

Ожидаемый вывод:

```text
/usr/sbin/dhcpd
```

Проверяем установленный пакет:

```bash
dpkg -l isc-dhcp-server
```

Ожидаем строку, начинающуюся с `ii`:

```text
ii  isc-dhcp-server  ...  amd64  ISC DHCP server for automatic IP address assignment
```

### Способ проверки

Выполняем:

```bash
dhcpd --version
```

Пример вывода:

```text
isc-dhcpd-4.4.x
```

Номер версии может отличаться.

---

## 11. Резервные копии конфигурационных файлов

Перед изменением настроек сохраняем исходные файлы.

```bash
sudo cp /etc/dhcp/dhcpd.conf /etc/dhcp/dhcpd.conf.bak
```

```bash
sudo cp /etc/default/isc-dhcp-server /etc/default/isc-dhcp-server.bak
```

Проверяем наличие копий:

```bash
ls -l /etc/dhcp/dhcpd.conf*
```

Ожидаемый вывод содержит два файла:

```text
-rw-r--r-- 1 root root ... /etc/dhcp/dhcpd.conf
-rw-r--r-- 1 root root ... /etc/dhcp/dhcpd.conf.bak
```

Аналогично проверяем второй файл:

```bash
ls -l /etc/default/isc-dhcp-server*
```

Если позже понадобится вернуть исходный вариант:

```bash
sudo cp /etc/dhcp/dhcpd.conf.bak /etc/dhcp/dhcpd.conf
```

---

## 12. Выбор интерфейса DHCP-сервера

ISC DHCP Server должен слушать только внутренний интерфейс `enp0s8`.

Открываем файл:

```bash
sudo nano /etc/default/isc-dhcp-server
```

Находим строку:

```text
INTERFACESv4=""
```

Указываем внутренний интерфейс:

```text
INTERFACESv4="enp0s8"
```

Строку IPv6 оставляем пустой:

```text
INTERFACESv6=""
```

Итоговый фрагмент:

```text
INTERFACESv4="enp0s8"
INTERFACESv6=""
```

### Почему не указываем NAT-интерфейс

На `enp0s3` работает сеть VirtualBox NAT. Наш сервер не должен раздавать адреса в этот сегмент. DHCP обслуживает только лабораторную сеть `dhcp-lab` через `enp0s8`.

### Способ проверки

Показываем активные строки файла:

```bash
grep -E '^INTERFACESv[46]' /etc/default/isc-dhcp-server
```

Ожидаемый вывод:

```text
INTERFACESv4="enp0s8"
INTERFACESv6=""
```

Дополнительно убеждаемся, что у интерфейса есть статический адрес:

```bash
ip -4 address show dev enp0s8
```

Ожидаем:

```text
inet 192.168.10.10/24 brd 192.168.10.255 scope global enp0s8
```

---

## 13. Настройка DHCP pool, lease и options

Основной файл находится здесь:

```text
/etc/dhcp/dhcpd.conf
```

Открываем его:

```bash
sudo nano /etc/dhcp/dhcpd.conf
```

Для лаборатории можно заменить содержимое следующим вариантом:

```text
ddns-update-style none;
authoritative;

default-lease-time 600;
max-lease-time 7200;

subnet 192.168.10.0 netmask 255.255.255.0 {
    range 192.168.10.100 192.168.10.150;

    option subnet-mask 255.255.255.0;
    option broadcast-address 192.168.10.255;
    option routers 192.168.10.10;
    option domain-name-servers 1.1.1.1, 8.8.8.8;
    option domain-name "lab.local";
}
```

### Разбираем параметры

#### `ddns-update-style none;`

Отключает динамическое обновление DNS. В этой лаборатории отдельный DNS-сервер не настраивается.

#### `authoritative;`

Сообщает, что этот сервер является основным DHCP-сервером своей подсети.

#### `default-lease-time 600;`

Задаёт обычное время аренды в секундах:

```text
600 секунд = 10 минут
```

#### `max-lease-time 7200;`

Ограничивает максимальную аренду:

```text
7200 секунд = 2 часа
```

#### `subnet ... netmask ...`

Описывает подсеть, которую обслуживает сервер:

```text
192.168.10.0/24
```

#### `range`

Задаёт динамический pool:

```text
192.168.10.100–192.168.10.150
```

В диапазоне 51 адрес.

#### `option subnet-mask`

Передаёт клиенту маску:

```text
255.255.255.0
```

#### `option broadcast-address`

Передаёт broadcast-адрес:

```text
192.168.10.255
```

#### `option routers`

Передаёт адрес gateway:

```text
192.168.10.10
```

В основной части работы проверяем именно получение параметра. Для выхода клиента в интернет серверу дополнительно понадобятся маршрутизация и NAT, которые здесь не настраиваются.

#### `option domain-name-servers`

Передаёт два DNS-сервера:

```text
1.1.1.1
8.8.8.8
```

#### `option domain-name`

Передаёт DNS-суффикс:

```text
lab.local
```

---

## 14. Проверка синтаксиса конфигурации

До перезапуска службы всегда проверяем файл:

```bash
sudo dhcpd -t -cf /etc/dhcp/dhcpd.conf
```

### Ожидаемый результат

Если синтаксис правильный, команда не показывает сообщений об ошибках. В некоторых версиях перед возвратом приглашения может появиться короткая информация о версии DHCP Server:

```text
admin@dhcp01:~$
```

Проверяем код завершения:

```bash
echo $?
```

Ожидаемый вывод:

```text
0
```

### Пример ошибки

Если забыть точку с запятой после `range`, можно увидеть сообщение вида:

```text
/etc/dhcp/dhcpd.conf line 8: semicolon expected.
    option subnet-mask
    ^
Configuration file errors encountered -- exiting
```

В этом случае службу не перезапускаем. Сначала исправляем указанную строку и снова выполняем `dhcpd -t`.

### Дополнительная проверка подсети

Сверяем адрес сервера:

```bash
ip -4 address show enp0s8
```

и описание подсети в `dhcpd.conf`:

```bash
grep -A 12 '^subnet' /etc/dhcp/dhcpd.conf
```

Адрес `192.168.10.10/24` должен относиться к сети `192.168.10.0/24`.

---

## 15. Запуск DHCP-службы

Перезапускаем службу:

```bash
sudo systemctl restart isc-dhcp-server
```

Включаем автоматический запуск при загрузке:

```bash
sudo systemctl enable isc-dhcp-server
```

Проверяем состояние:

```bash
systemctl status isc-dhcp-server --no-pager
```

Ожидаем строки:

```text
● isc-dhcp-server.service - ISC DHCP IPv4 server
     Loaded: loaded (...; enabled; preset: enabled)
     Active: active (running)
```

Главный признак успеха:

```text
Active: active (running)
```

Проверяем коротко:

```bash
systemctl is-active isc-dhcp-server
```

Ожидаемый вывод:

```text
active
```

Проверяем автозапуск:

```bash
systemctl is-enabled isc-dhcp-server
```

Ожидаемый вывод:

```text
enabled
```

### Если служба не запустилась

Смотрим последние сообщения:

```bash
sudo journalctl -u isc-dhcp-server -n 50 --no-pager
```

Частые сообщения:

```text
No subnet declaration for enp0s8
```

Это означает, что адрес интерфейса и блок `subnet` не совпадают.

```text
Not configured to listen on any interfaces
```

Это означает, что неверно заполнена строка `INTERFACESv4` или нужный интерфейс не имеет IPv4-адреса.

---

## 16. Проверка UDP-порта 67

DHCP-сервер принимает запросы на UDP-порт 67.

Проверяем слушающий сокет:

```bash
sudo ss -lunp | grep ':67'
```

Пример ожидаемого вывода:

```text
UNCONN 0 0 0.0.0.0:67 0.0.0.0:* users:(("dhcpd",pid=1234,fd=7))
```

Номер процесса будет другим.

Проверяем сам процесс:

```bash
ps -ef | grep '[d]hcpd'
```

Пример ожидаемого вывода:

```text
dhcpd  1234  1  0 10:25 ?  00:00:00 /usr/sbin/dhcpd -4 -q -cf /etc/dhcp/dhcpd.conf enp0s8
```

В конце строки должен быть внутренний интерфейс:

```text
enp0s8
```

### Способ проверки

Три команды должны давать согласованный результат:

```bash
systemctl is-active isc-dhcp-server
sudo ss -lunp | grep ':67'
ps -ef | grep '[d]hcpd'
```

Служба должна быть активна, порт 67 — открыт процессом `dhcpd`, а процесс — работать на внутреннем интерфейсе.

---

## 17. Запрос адреса на Ubuntu-клиенте

Переходим в консоль `client01`.

Проверяем текущий адрес:

```bash
ip -br address show enp0s3
```

Обновляем сетевую конфигурацию:

```bash
sudo netplan apply
```

Если интерфейс управляется `systemd-networkd`, можно принудительно запросить новую аренду:

```bash
sudo networkctl renew enp0s3
```

После этого снова проверяем адрес:

```bash
ip -br address show enp0s3
```

Ожидаемый вывод:

```text
enp0s3           UP             192.168.10.100/24
```

Клиент может получить любой свободный адрес из диапазона:

```text
192.168.10.100–192.168.10.150
```

Если команда `networkctl renew` сообщает, что интерфейс не управляется `systemd-networkd`, достаточно выполнить:

```bash
sudo netplan apply
```

или перезагрузить клиент:

```bash
sudo reboot
```

---

## 18. Запрос адреса на Debian-клиенте

Переходим в консоль `client01` с Debian.

Перезапускаем интерфейс:

```bash
sudo ifdown enp0s3
sudo ifup enp0s3
```

Если `ifdown` сообщает, что интерфейс не настроен, применяем:

```bash
sudo systemctl restart networking
```

Проверяем адрес:

```bash
ip -br address show enp0s3
```

Ожидаемый вывод:

```text
enp0s3           UP             192.168.10.100/24
```

### Дополнительный вариант с `dhclient`

Если на клиенте установлен пакет `isc-dhcp-client`, можно освободить и запросить аренду явно:

```bash
sudo dhclient -r enp0s3
sudo dhclient -v enp0s3
```

В подробном выводе обычно видны этапы DORA:

```text
DHCPDISCOVER on enp0s3 to 255.255.255.255 port 67
DHCPOFFER of 192.168.10.100 from 192.168.10.10
DHCPREQUEST for 192.168.10.100 on enp0s3
DHCPACK of 192.168.10.100 from 192.168.10.10
bound to 192.168.10.100 -- renewal in ... seconds
```

---

## 19. Проверка выданных параметров на Linux-клиенте

### 19.1. Проверяем IP-адрес

```bash
ip -4 address show enp0s3
```

Ожидаем строку вида:

```text
inet 192.168.10.100/24 brd 192.168.10.255 scope global dynamic enp0s3
```

Важные признаки:

- адрес находится в pool;
- маска `/24` правильная;
- присутствует слово `dynamic`.

### 19.2. Проверяем маршрут

```bash
ip route
```

Пример ожидаемого вывода:

```text
default via 192.168.10.10 dev enp0s3 proto dhcp src 192.168.10.100
192.168.10.0/24 dev enp0s3 proto kernel scope link src 192.168.10.100
```

Строка `default via 192.168.10.10` подтверждает получение `option routers`.

### 19.3. Проверяем DNS на Ubuntu

```bash
resolvectl status enp0s3
```

Ожидаем фрагмент:

```text
Link 2 (enp0s3)
    Current Scopes: DNS
         DNS Servers: 1.1.1.1 8.8.8.8
          DNS Domain: lab.local
```

### 19.4. Проверяем DNS на Debian

```bash
cat /etc/resolv.conf
```

В зависимости от используемого сетевого менеджера вывод может выглядеть так:

```text
search lab.local
nameserver 1.1.1.1
nameserver 8.8.8.8
```

### 19.5. Проверяем связь с DHCP-сервером

```bash
ping -c 3 192.168.10.10
```

Ожидаемый итог:

```text
3 packets transmitted, 3 received, 0% packet loss
```

### Важное уточнение

Клиент может получить IP, gateway и DNS, но не иметь доступа в интернет. Это не означает ошибку DHCP. DHCP только передаёт параметры. Для выхода через `dhcp01` дополнительно нужна настройка маршрутизации и NAT.

---

## 20. Проверка lease-файла на сервере

Возвращаемся на `dhcp01`.

ISC DHCP Server хранит сведения об арендах в файле:

```text
/var/lib/dhcp/dhcpd.leases
```

Смотрим последние записи:

```bash
sudo tail -n 40 /var/lib/dhcp/dhcpd.leases
```

Пример ожидаемого блока:

```text
lease 192.168.10.100 {
  starts 4 2026/07/16 10:30:12;
  ends 4 2026/07/16 10:40:12;
  cltt 4 2026/07/16 10:30:12;
  binding state active;
  next binding state free;
  hardware ethernet 08:00:27:aa:bb:cc;
  client-hostname "client01";
}
```

### Что означает запись

- `lease 192.168.10.100` — выданный адрес;
- `starts` — начало аренды;
- `ends` — окончание аренды;
- `binding state active` — аренда активна;
- `hardware ethernet` — MAC-адрес клиента;
- `client-hostname` — имя клиента, если оно передано.

Ищем активные аренды:

```bash
sudo grep -n 'binding state active' /var/lib/dhcp/dhcpd.leases
```

Ищем конкретный MAC-адрес:

```bash
sudo grep -in '08:00:27:aa:bb:cc' /var/lib/dhcp/dhcpd.leases
```

Подставляем реальный MAC клиента.

### Способ проверки

Адрес в lease-файле должен совпадать с адресом команды на клиенте:

```bash
ip -4 address show enp0s3
```

MAC в lease-файле должен совпадать с:

```bash
ip link show enp0s3
```

---

## 21. Проверка журналов DHCP

Смотрим последние записи службы:

```bash
sudo journalctl -u isc-dhcp-server -n 50 --no-pager
```

При успешной выдаче адреса ожидаем четыре типа сообщений:

```text
DHCPDISCOVER from 08:00:27:aa:bb:cc via enp0s8
DHCPOFFER on 192.168.10.100 to 08:00:27:aa:bb:cc via enp0s8
DHCPREQUEST for 192.168.10.100 from 08:00:27:aa:bb:cc via enp0s8
DHCPACK on 192.168.10.100 to 08:00:27:aa:bb:cc via enp0s8
```

Это и есть DORA:

| Этап | Сообщение | Смысл |
|---|---|---|
| D | Discover | клиент ищет DHCP-сервер |
| O | Offer | сервер предлагает адрес |
| R | Request | клиент запрашивает предложенный адрес |
| A | Acknowledge | сервер подтверждает аренду |

Для наблюдения в реальном времени открываем журнал:

```bash
sudo journalctl -fu isc-dhcp-server
```

После этого на клиенте обновляем интерфейс. Новые сообщения должны появиться сразу.

Выход из режима наблюдения:

```text
Ctrl+C
```

### Способ проверки

Успешная выдача подтверждается не одной, а несколькими точками:

1. клиент получил адрес;
2. в lease-файле есть активная аренда;
3. в журнале есть DORA;
4. сервер отвечает на `ping`.

---

## 22. Просмотр DHCP-пакетов через tcpdump

Журнал показывает, что обработала служба. `tcpdump` показывает, какие пакеты реально проходят через интерфейс.

На сервере запускаем:

```bash
sudo tcpdump -ni enp0s8 -vvv 'port 67 or port 68'
```

Где:

- `-n` запрещает преобразование адресов в имена;
- `-i enp0s8` выбирает внутренний интерфейс;
- `-vvv` включает подробный вывод;
- фильтр оставляет только DHCP-трафик.

Оставляем команду работающей.

На клиенте обновляем аренду.

Для Ubuntu:

```bash
sudo networkctl renew enp0s3
```

Для Debian:

```bash
sudo ifdown enp0s3 && sudo ifup enp0s3
```

Пример сокращённого вывода `tcpdump`:

```text
IP 0.0.0.0.68 > 255.255.255.255.67: BOOTP/DHCP, Request from 08:00:27:aa:bb:cc
      DHCP-Message Option 53, length 1: Discover

IP 192.168.10.10.67 > 192.168.10.100.68: BOOTP/DHCP, Reply
      DHCP-Message Option 53, length 1: Offer

IP 0.0.0.0.68 > 255.255.255.255.67: BOOTP/DHCP, Request
      DHCP-Message Option 53, length 1: Request

IP 192.168.10.10.67 > 192.168.10.100.68: BOOTP/DHCP, Reply
      DHCP-Message Option 53, length 1: ACK
```

Формат вывода зависит от версии `tcpdump`, но должны различаться сообщения Discover, Offer, Request и ACK.

Останавливаем захват:

```text
Ctrl+C
```

### Как читать результат

| Что видно | Вывод |
|---|---|
| Нет Discover | клиент не отправляет запрос или находится в другой виртуальной сети |
| Есть Discover, нет Offer | сервер не отвечает: проверяем службу, интерфейс и pool |
| Есть Offer, нет Request | клиент не принял предложение или получил предложение другого сервера |
| Есть Request, нет ACK | сервер не подтвердил адрес: проверяем конфигурацию и журналы |
| Видна полная DORA | сетевой обмен DHCP завершён успешно |

---

## 23. Настройка reservation по MAC-адресу

Теперь закрепим за `client01` адрес:

```text
192.168.10.50
```

Этот адрес находится вне динамического pool.

### 23.1. Узнаём MAC-адрес клиента

На клиенте:

```bash
ip link show enp0s3
```

Пример фрагмента:

```text
link/ether 08:00:27:aa:bb:cc brd ff:ff:ff:ff:ff:ff
```

Копируем адрес после `link/ether`.

Можно вывести только MAC:

```bash
cat /sys/class/net/enp0s3/address
```

Ожидаемый вывод:

```text
08:00:27:aa:bb:cc
```

### 23.2. Добавляем reservation

На сервере открываем конфигурацию:

```bash
sudo nano /etc/dhcp/dhcpd.conf
```

Внутрь блока `subnet`, после options, добавляем:

```text
    host client01 {
        hardware ethernet 08:00:27:aa:bb:cc;
        fixed-address 192.168.10.50;
    }
```

Полный блок подсети будет выглядеть так:

```text
subnet 192.168.10.0 netmask 255.255.255.0 {
    range 192.168.10.100 192.168.10.150;

    option subnet-mask 255.255.255.0;
    option broadcast-address 192.168.10.255;
    option routers 192.168.10.10;
    option domain-name-servers 1.1.1.1, 8.8.8.8;
    option domain-name "lab.local";

    host client01 {
        hardware ethernet 08:00:27:aa:bb:cc;
        fixed-address 192.168.10.50;
    }
}
```

Подставляем фактический MAC клиента.

### 23.3. Проверяем и применяем конфигурацию

Проверяем синтаксис:

```bash
sudo dhcpd -t -cf /etc/dhcp/dhcpd.conf
```

Ожидаемый результат — отсутствие ошибок.

Перезапускаем службу:

```bash
sudo systemctl restart isc-dhcp-server
```

Проверяем:

```bash
systemctl is-active isc-dhcp-server
```

Ожидаемый вывод:

```text
active
```

### 23.4. Запрашиваем адрес заново

На Ubuntu-клиенте:

```bash
sudo networkctl renew enp0s3
```

Если старый адрес сохранился, перезапускаем клиент:

```bash
sudo reboot
```

На Debian-клиенте:

```bash
sudo ifdown enp0s3
sudo ifup enp0s3
```

При наличии `dhclient`:

```bash
sudo dhclient -r enp0s3
sudo dhclient -v enp0s3
```

Проверяем адрес:

```bash
ip -br address show enp0s3
```

Ожидаемый вывод:

```text
enp0s3           UP             192.168.10.50/24
```

### 23.5. Проверяем reservation на сервере

Смотрим журнал:

```bash
sudo journalctl -u isc-dhcp-server -n 30 --no-pager
```

Ожидаем DHCPACK для адреса `192.168.10.50`:

```text
DHCPACK on 192.168.10.50 to 08:00:27:aa:bb:cc via enp0s8
```

Проверяем связь:

```bash
ping -c 3 192.168.10.50
```

Ожидаемый итог:

```text
3 packets transmitted, 3 received, 0% packet loss
```

---

## 24. Проверка нескольких клиентов

Если есть `client02`, подключаем его к той же внутренней сети `dhcp-lab` и включаем DHCP.

После загрузки смотрим адрес:

```bash
ip -br address
```

Он должен получить свободный адрес из динамического pool, например:

```text
enp0s3           UP             192.168.10.100/24
```

При этом `client01` сохраняет reservation:

```text
192.168.10.50
```

На сервере смотрим активные аренды:

```bash
sudo grep -B 4 -A 6 'binding state active' /var/lib/dhcp/dhcpd.leases
```

В lease-файле может быть несколько исторических блоков для одного адреса. Это нормально: сервер не обязан удалять старые записи сразу. Ориентируемся на последние записи и актуальные даты.

### Способ проверки

На каждом клиенте смотрим:

```bash
ip -4 address
```

Адреса не должны совпадать.

На сервере проверяем журнал:

```bash
sudo journalctl -u isc-dhcp-server -n 80 --no-pager | grep -E 'DHCPACK|DHCPOFFER'
```

Должны быть видны разные MAC-адреса и разные выданные IP.

---

## 25. Дополнительная проверка с Windows-клиента

Подключаем сетевой адаптер Windows к внутренней сети:

```text
dhcp-lab
```

В Windows открываем командную строку от имени администратора.

Освобождаем старую аренду:

```powershell
ipconfig /release
```

Запрашиваем новую:

```powershell
ipconfig /renew
```

Смотрим подробную конфигурацию:

```powershell
ipconfig /all
```

Ожидаемые параметры:

```text
DHCP Enabled. . . . . . . . . . . : Yes
IPv4 Address. . . . . . . . . . . : 192.168.10.100
Subnet Mask . . . . . . . . . . . : 255.255.255.0
Default Gateway . . . . . . . . . : 192.168.10.10
DHCP Server . . . . . . . . . . . : 192.168.10.10
DNS Servers . . . . . . . . . . . : 1.1.1.1
                                      8.8.8.8
```

Проверяем связь с сервером:

```powershell
ping 192.168.10.10
```

Ожидаем ответы без потерь.

Если Windows продолжает использовать старые данные, очищаем ARP- и DNS-кэш:

```powershell
arp -d *
ipconfig /flushdns
```

Затем повторяем `ipconfig /release` и `ipconfig /renew`.

---

## 26. Проверка времени аренды

В конфигурации задано:

```text
default-lease-time 600;
max-lease-time 7200;
```

На сервере смотрим lease:

```bash
sudo tail -n 30 /var/lib/dhcp/dhcpd.leases
```

Пример:

```text
starts 4 2026/07/16 10:30:12;
ends 4 2026/07/16 10:40:12;
```

Разница составляет 10 минут, то есть 600 секунд.

На Windows время аренды смотрим через:

```powershell
ipconfig /all
```

В выводе будут строки:

```text
Lease Obtained. . . . . . . . . . : ...
Lease Expires . . . . . . . . . . : ...
```

На Linux точный способ зависит от сетевого менеджера. На Ubuntu с `systemd-networkd` смотрим:

```bash
networkctl status enp0s3
```

В выводе могут отображаться сведения о DHCPv4, сервере и времени аренды.

### Эксперимент

Меняем обычную аренду на 300 секунд:

```text
default-lease-time 300;
```

Проверяем синтаксис и перезапускаем службу:

```bash
sudo dhcpd -t -cf /etc/dhcp/dhcpd.conf
sudo systemctl restart isc-dhcp-server
```

Обновляем аренду клиента и снова сравниваем `starts` и `ends`.

После проверки возвращаем значение:

```text
default-lease-time 600;
```

---

## 27. Проверка ограничения одной подсети

DHCP Discover отправляется как broadcast. Маршрутизаторы обычно не пересылают такой broadcast между подсетями без DHCP relay.

Проведём простой эксперимент.

### 27.1. Создаём вторую внутреннюю сеть

В VirtualBox у клиента временно меняем имя внутренней сети:

```text
dhcp-lab-2
```

Сервер остаётся в сети:

```text
dhcp-lab
```

Запускаем на сервере захват:

```bash
sudo tcpdump -ni enp0s8 'port 67 or port 68'
```

На клиенте обновляем интерфейс.

### Ожидаемый результат

На сервере не появляются DHCP Discover от этого клиента, потому что виртуальные машины находятся в разных L2-сегментах.

Клиент не получает адрес из `192.168.10.0/24`.

Возвращаем клиент в сеть:

```text
dhcp-lab
```

Снова обновляем интерфейс. DHCP должен заработать.

### Вывод из эксперимента

Один DHCP-сервер без relay обслуживает клиентов своего широковещательного домена. Для другой VLAN или подсети нужен:

- отдельный DHCP-сервер;
- либо DHCP relay на маршрутизаторе или L3-коммутаторе.

---

## 28. Проверка после перезагрузки

Перезагружаем сервер:

```bash
sudo reboot
```

После загрузки проверяем статический адрес:

```bash
ip -br address show enp0s8
```

Ожидаемый вывод:

```text
enp0s8           UP             192.168.10.10/24
```

Проверяем службу:

```bash
systemctl is-active isc-dhcp-server
```

Ожидаемый вывод:

```text
active
```

Проверяем порт:

```bash
sudo ss -lunp | grep ':67'
```

Перезагружаем клиент или обновляем его интерфейс.

Проверяем адрес:

```bash
ip -br address
```

Для `client01` с reservation ожидаем:

```text
192.168.10.50/24
```

Проверяем связь:

```bash
ping -c 3 192.168.10.10
```

### Способ проверки

После перезагрузки должны сохраняться:

- статический адрес сервера;
- автоматический запуск DHCP-службы;
- динамический pool;
- DHCP options;
- reservation клиента.

---

## 29. Диагностика: идём по уровням

Когда клиент не получает адрес, не меняем настройки наугад. Проверяем систему последовательно.

### Уровень 1. VirtualBox

Проверяем:

- обе машины включены;
- кабель подключён;
- имена внутренних сетей совпадают;
- клиент не находится в другой сети;
- в сегменте нет лишнего DHCP-сервера.

### Уровень 2. Интерфейсы

На сервере:

```bash
ip -br link
ip -br address
```

На клиенте:

```bash
ip -br link
ip -br address
```

Интерфейсы должны быть `UP`.

### Уровень 3. Адрес сервера

```bash
ip -4 address show enp0s8
```

Должен быть адрес:

```text
192.168.10.10/24
```

### Уровень 4. Конфигурация

```bash
sudo dhcpd -t -cf /etc/dhcp/dhcpd.conf
```

Код завершения должен быть `0`:

```bash
echo $?
```

### Уровень 5. Служба

```bash
systemctl status isc-dhcp-server --no-pager
```

Нужно состояние:

```text
active (running)
```

### Уровень 6. Порт

```bash
sudo ss -lunp | grep ':67'
```

Должен быть процесс `dhcpd`.

### Уровень 7. DHCP-пакеты

```bash
sudo tcpdump -ni enp0s8 'port 67 or port 68'
```

Смотрим, появляется ли Discover.

### Уровень 8. Журналы

```bash
sudo journalctl -u isc-dhcp-server -n 50 --no-pager
```

Ищем DORA или сообщения об ошибках.

### Уровень 9. Lease

```bash
sudo tail -n 40 /var/lib/dhcp/dhcpd.leases
```

Проверяем адрес, MAC и состояние аренды.

### Уровень 10. Полученные options

На клиенте:

```bash
ip route
resolvectl status
```

или:

```bash
cat /etc/resolv.conf
```

---

## 30. Типовые неисправности и решения

### Проблема 1. Служба не запускается

Проверяем:

```bash
sudo dhcpd -t -cf /etc/dhcp/dhcpd.conf
sudo journalctl -u isc-dhcp-server -n 50 --no-pager
```

Частая причина — пропущенная точка с запятой или лишняя фигурная скобка.

### Проблема 2. `Not configured to listen on any interfaces`

Проверяем:

```bash
grep '^INTERFACESv4' /etc/default/isc-dhcp-server
ip -4 address
```

В `INTERFACESv4` должно быть правильное имя интерфейса, а у интерфейса — статический IPv4-адрес.

### Проблема 3. `No subnet declaration for enp0s8`

Проверяем соответствие:

```text
Интерфейс: 192.168.10.10/24
Конфигурация: subnet 192.168.10.0 netmask 255.255.255.0
```

Они должны относиться к одной подсети.

### Проблема 4. Клиент не отправляет Discover

Проверяем VirtualBox и интерфейс клиента:

```bash
ip link show
```

Проверяем захват на сервере:

```bash
sudo tcpdump -ni enp0s8 'port 67 or port 68'
```

Если пакетов нет, проблема находится до DHCP-службы.

### Проблема 5. Discover виден, Offer отсутствует

Проверяем:

```bash
systemctl is-active isc-dhcp-server
sudo journalctl -u isc-dhcp-server -n 50 --no-pager
```

Также проверяем, не закончился ли pool и подходит ли subnet.

### Проблема 6. Клиент получил адрес не из нашего pool

Ищем второй DHCP-сервер.

На клиенте смотрим адрес DHCP-сервера:

- Linux: `networkctl status` или журнал клиента;
- Windows: `ipconfig /all`.

На сервере смотрим пакеты:

```bash
sudo tcpdump -ni enp0s8 -vvv 'port 67 or port 68'
```

Если Offer приходит от другого IP, в сегменте есть другой DHCP-сервис.

### Проблема 7. Адрес получен, но gateway неверный

Проверяем строку:

```text
option routers ...;
```

После изменения:

```bash
sudo dhcpd -t -cf /etc/dhcp/dhcpd.conf
sudo systemctl restart isc-dhcp-server
```

Затем обновляем аренду клиента.

### Проблема 8. Адрес получен, но DNS неверный

Проверяем:

```text
option domain-name-servers ...;
```

На клиенте смотрим:

```bash
resolvectl status
```

или:

```bash
cat /etc/resolv.conf
```

### Проблема 9. Reservation не работает

Проверяем:

- точность MAC-адреса;
- адрес `fixed-address`;
- расположение reservation в нужном блоке `subnet`;
- отсутствие старой аренды у клиента;
- синтаксис файла.

Команды:

```bash
cat /sys/class/net/enp0s3/address
sudo dhcpd -t -cf /etc/dhcp/dhcpd.conf
sudo journalctl -u isc-dhcp-server -n 50 --no-pager
```

### Проблема 10. После изменения клиент сохраняет старые параметры

Обновляем аренду.

Ubuntu:

```bash
sudo networkctl renew enp0s3
```

Debian:

```bash
sudo ifdown enp0s3 && sudo ifup enp0s3
```

С `dhclient`:

```bash
sudo dhclient -r enp0s3
sudo dhclient -v enp0s3
```

Windows:

```powershell
ipconfig /release
ipconfig /renew
```

---

## 31. Короткий набор команд для быстрой проверки

### На сервере

```bash
hostnamectl --static
ip -br address
cat /etc/default/isc-dhcp-server | grep '^INTERFACESv4'
sudo dhcpd -t -cf /etc/dhcp/dhcpd.conf
systemctl is-active isc-dhcp-server
systemctl is-enabled isc-dhcp-server
sudo ss -lunp | grep ':67'
sudo tail -n 30 /var/lib/dhcp/dhcpd.leases
sudo journalctl -u isc-dhcp-server -n 30 --no-pager
```

### На Linux-клиенте

```bash
ip -br address
ip route
ping -c 3 192.168.10.10
resolvectl status 2>/dev/null || cat /etc/resolv.conf
```

### Что должно получиться

- сервер имеет адрес `192.168.10.10/24`;
- служба активна и включена в автозапуск;
- процесс `dhcpd` слушает UDP 67;
- клиент получил адрес из pool или reservation;
- default route указывает на `192.168.10.10`;
- DNS-параметры получены;
- в журнале есть DORA;
- в lease-файле есть MAC и адрес клиента.

---

## 32. Итоговый чек-лист самопроверки

### VirtualBox

- [ ] Сервер и клиент подключены к внутренней сети `dhcp-lab`.
- [ ] На сервере NAT и внутренняя сеть подключены к разным адаптерам.
- [ ] На клиенте включён режим DHCP.
- [ ] В лабораторном сегменте нет второго DHCP-сервера.

### Сервер

- [ ] Хост называется `dhcp01`.
- [ ] Внутренний интерфейс имеет адрес `192.168.10.10/24`.
- [ ] В `INTERFACESv4` указан правильный интерфейс.
- [ ] В `dhcpd.conf` описана сеть `192.168.10.0/24`.
- [ ] Pool равен `192.168.10.100–192.168.10.150`.
- [ ] Синтаксис конфигурации проходит проверку.
- [ ] Служба `isc-dhcp-server` активна.
- [ ] UDP-порт 67 слушается процессом `dhcpd`.

### Клиент

- [ ] Клиент получает адрес автоматически.
- [ ] Динамический адрес находится внутри pool.
- [ ] Маска равна `/24`.
- [ ] Gateway равен `192.168.10.10`.
- [ ] Получены DNS `1.1.1.1` и `8.8.8.8`.
- [ ] Получен доменный суффикс `lab.local`.
- [ ] `client01` после reservation получает `192.168.10.50`.

### Диагностика

- [ ] В журнале видны Discover, Offer, Request и ACK.
- [ ] В lease-файле есть активная аренда.
- [ ] `tcpdump` показывает DORA.
- [ ] После перезагрузки сервер продолжает выдавать адреса.

---

## 33. Что сохранить в отчёте

Отчёт можно оформить в Markdown или текстовом документе.

Рекомендуемая структура:

### 1. Схема стенда

Указываем:

- операционные системы;
- имена виртуальных машин;
- типы сетевых адаптеров;
- имя внутренней сети;
- адресный план.

### 2. Интерфейсы сервера

Добавляем вывод:

```bash
ip -br address
```

### 3. Конфигурация DHCP

Добавляем содержимое без комментариев и пустых строк:

```bash
grep -vE '^[[:space:]]*(#|$)' /etc/dhcp/dhcpd.conf
```

### 4. Выбор интерфейса

Добавляем вывод:

```bash
grep '^INTERFACESv4' /etc/default/isc-dhcp-server
```

### 5. Состояние службы

Добавляем вывод:

```bash
systemctl status isc-dhcp-server --no-pager
```

### 6. Параметры клиента

Добавляем вывод:

```bash
ip -br address
ip route
resolvectl status 2>/dev/null || cat /etc/resolv.conf
```

### 7. Lease

Добавляем актуальный блок из:

```bash
sudo tail -n 40 /var/lib/dhcp/dhcpd.leases
```

### 8. DORA

Добавляем четыре строки из журнала:

```bash
sudo journalctl -u isc-dhcp-server --no-pager | grep -E 'DHCPDISCOVER|DHCPOFFER|DHCPREQUEST|DHCPACK' | tail -n 8
```

### 9. Reservation

Фиксируем:

- MAC клиента;
- закреплённый адрес;
- результат `ip -br address` после обновления аренды.

### 10. Вывод

Кратко описываем:

- чем DHCP pool отличается от reservation;
- где хранится информация о lease;
- как подтвердить полную DORA;
- почему DHCP без relay не обслуживает другую подсеть.

---

## 34. Вопросы для самопроверки

1. Почему DHCP-серверу назначают статический адрес?
2. Чем внутренняя сеть VirtualBox удобнее сетевого моста для этой лаборатории?
3. Какой UDP-порт слушает DHCP-сервер?
4. Какой UDP-порт использует DHCP-клиент?
5. Что задаёт параметр `range`?
6. Почему адрес reservation лучше вынести за пределы динамического pool?
7. Чем `default-lease-time` отличается от `max-lease-time`?
8. Какие параметры передаются через `option routers` и `option domain-name-servers`?
9. Какая команда проверяет синтаксис `dhcpd.conf`?
10. Где ISC DHCP Server хранит аренды?
11. Какие четыре сообщения образуют DORA?
12. Что означает ситуация, когда `tcpdump` видит Discover, но не видит Offer?
13. Почему клиент может получить IP-адрес, но не иметь доступа в интернет?
14. Как проверить, от какого сервера Windows получила аренду?
15. Почему DHCP Discover обычно не проходит через маршрутизатор?
16. Для чего нужен DHCP relay?
17. Как определить наличие второго DHCP-сервера?
18. Что нужно проверить, если reservation не работает?

---

## 35. Итог

В результате работы настроен полноценный базовый DHCP-сценарий:

```text
VirtualBox → статический адрес сервера → ISC DHCP Server
→ pool → lease → options → клиентская аренда
→ reservation → журналы → tcpdump → диагностика
```

Настройка считается рабочей, когда выполняются все условия:

- сервер имеет постоянный адрес;
- служба запущена на правильном интерфейсе;
- клиент получает адрес из нужной подсети;
- gateway, DNS и domain name передаются правильно;
- lease сохраняется на сервере;
- DORA подтверждается журналом или `tcpdump`;
- reservation возвращает клиенту закреплённый адрес;
- после перезагрузки конфигурация продолжает работать.

Ключевой практический принцип:

> Сначала проверяем VirtualBox и интерфейсы, затем конфигурацию и службу, после этого смотрим пакеты, журналы и lease. Такой порядок быстрее приводит к причине неисправности, чем случайное изменение настроек.
