# AssetBundle
关于Assetbundle 的打包和加载功能

Editor文件下的BuildAssetBundle脚本，用于AssetBundle名称设置和打包移动。
使用方法：
1.	在LoadPathMgr里面指定Build_Path，即打包文件夹在Assets下面的路径。
2.	选择菜单栏里面的Build->Set AssetBundle Name 设置包名称
3.	选择对应平台进行打包，文件会打包到Assets同级目录下面的AssetBundleBuild下
4.	选择Build下面的Move指令，移动AssetBundleBuild下面的文件到StreamAssets下面。
（这里的第三步可以直接打包到StreamAssets下面，根据个人做法来吧）
