一个godot的启动器,有于 电脑中存在多版本godot编辑器时 启动对应版本,  不在桌面放置godot编辑器图标,
请自行手动添加到环境变量的Path之中,




<img width="696" height="450" alt="屏幕截图 2025-10-08 212553" src="https://github.com/user-attachments/assets/386e6447-9aac-4758-ae55-7c4a72307ebc" />
<img width="520" height="300" alt="屏幕截图 2025-10-08 212725" src="https://github.com/user-attachments/assets/da80ce17-48b2-43e3-9735-08b491e4b7f4" />
首次启动需要自行输入路径,

<img width="833" height="378" alt="屏幕截图 2025-10-08 212838" src="https://github.com/user-attachments/assets/85b90de1-3519-453a-a458-90546967d309" />
<img width="716" height="451" alt="屏幕截图 2025-10-08 212901" src="https://github.com/user-attachments/assets/b9efdfa4-68a9-4cde-937f-7f7124bb110b" />
需要保证对应文件夹中的可执行文件 开头为godot
比如   Godot_v4.6-dev1_mono_win64   跟Godot_v4.6-dev1_mono_win64_console  
通过字符串比对 获取到 console/非console 执行程序
<img width="621" height="449" alt="屏幕截图 2025-10-08 213045" src="https://github.com/user-attachments/assets/7848ef7a-ba59-4e4c-a940-9dfd224f0f7b" />

第一次执行后会保存为默认设置,需要 在启动时添加 -n 参数 屏蔽默认启动版本(console  非console ),
<img width="589" height="307" alt="屏幕截图 2025-10-08 213258" src="https://github.com/user-attachments/assets/7f1d33d4-03fa-494a-a97a-aaa8f70249dd" />
这是无参,


<img width="509" height="291" alt="屏幕截图 2025-10-08 213347" src="https://github.com/user-attachments/assets/85eb0b57-079d-429b-a36f-28c243c95cd2" />
这是入参,
