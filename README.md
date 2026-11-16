# 🌐 IPTray

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-blue.svg)](https://microsoft.com/windows)
[![Size](https://img.shields.io/badge/Binary%20Size-%3C%2025%20KB-success.svg)]()
[![Dependencies](https://img.shields.io/badge/Dependencies-Zero%20(Native)-brightgreen.svg)]()
[![License](https://img.shields.io/badge/License-MIT-orange.svg)](LICENSE)

An ultra-lightweight, standalone Windows 10 & 11 Taskbar Widget that displays your active **Public IP address, Country, and City/Region live directly on the Windows Taskbar**, with full **Connection History tracking**.

یک ویجت فوق‌العاده سبک و مستقل برای نوار وظیفه (تسک‌بار) ویندوز ۱۰ و ۱۱ جهت نمایش زنده **آی‌پی عمومی، کشور و شهر/منطقه** به همراه **تاریخچه کامل اتصالات و تغییرات شبکه**.

---

## 📸 Preview / پیش‌نمایش

```text
+-------------------------------------------------------------+
|  Windows 11 Taskbar                                         |
|  [Start] [Search]  ...      +-----------------------------+ |
|                             | 🟢 45.38.198.105       [LV] | |  12:50 PM
|                             |    Riga, Latvia             | |  2026/11/16
|                             +-----------------------------+ |
+-------------------------------------------------------------+
```

---

## 🇬🇧 English

### ✨ Key Features
- **Direct Taskbar Docking:** Sits seamlessly on your Windows 11 / 10 taskbar as a clean, modern translucent pill without hiding inside the overflow menu.
- **Two-Line Information Display:**
  - **Top Line:** Status Dot + IP Address + Country Code badge (`[LV]`, `[DE]`, `[US]`).
  - **Bottom Line:** Region & City name (`Riga, Latvia`).
- **Connection & Location History:** Clicking the widget opens an interactive dark flyout window tracking your IP and location history with timestamps, cities, countries, and ISPs.
- **Instant VPN / Network Detection:** Listens to Windows network interface change events (`NetworkAddressChanged`). The moment you connect or disconnect your VPN, the widget updates immediately.
- **Click to Copy:** Quick-copy button in the history window, or double-click any historical IP to copy it to your clipboard.
- **Draggable & Dockable:** Hold left-click and drag the widget anywhere along the taskbar. Your preferred location is saved automatically.
- **Zero Runtime Dependencies:** Built using native Win32 & .NET Framework APIs pre-installed in Windows. No installer or runtimes needed.
- **Tiny Footprint:** File size is **under 25 KB** with ~20 MB RAM usage and 0% CPU.

---

## 🇮🇷 فارسی (Persian)

### ✨ ویژگی‌های کلیدی
- **نمایش مستقیم روی نوار وظیفه (تسک‌بار):** بدون نیاز به باز کردن منوی مخفی (`^`)، اطلاعات آی‌پی و لوکیشن در یک کپسول شیشه‌ای روی نوار وظیفه قرار دارد.
- **نمایش دوخطی:**
  - **خط بالا:** چراغ وضعیت + آی‌پی + کد کشور (`[LV]`).
  - **خط پایین:** نام شهر و کشور/منطقه (`Riga, Latvia`).
- **پنجره تاریخچه اتصالات (History):** با یک کلیک روی ویجت، پنجره تاریخچه دقیقاً بالای نوار باز می‌شود و تمام تغییرات آی‌پی و منطقه را همراه با تاریخ و ساعت دقیق، نام شهر، کشور و شرکت ارائه‌دهنده (ISP) نمایش می‌دهد.
- **دابل‌کلیک برای کپی در تاریخچه:** در لیست تاریخچه، با دابل‌کلیک روی هر ردیف، آی‌پی آن زمان فوراً در کلیپ‌بورد کپی می‌شود.
- **تشخیص هوشمند فیلترشکن:** به رویدادهای شبکه ویندوز گوش می‌دهد و تغییرات را بی‌درنگ ثبت می‌کند.
- **جابجایی با ماوس (Drag & Drop):** می‌توانید ویجت را با ماوس به هر نقطه از تسک‌بار ببرید.
- **حجم فوق‌العاده ناچیز:** فایل مستقل حدود **۲۳ کیلوبایت** بدون نیاز به نصب هیچ پیش‌نیازی.

### 🔨 نحوه ساخت از سورس (Build)
کافیست فایل `build.bat` را اجرا کنید یا دستور زیر را در خط فرمان بزنید:

```cmd
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /optimize /out:IPTray.exe Program.cs
```

---

## 📄 License
This project is licensed under the [MIT License](LICENSE).
