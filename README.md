# AssetBundle
关于Assetbundle 的打包和加载功能

一.打包AssetBundle

Editor文件下的BuildAssetBundle脚本
主要功能：
1.	通过指定文件路径，设置路径下面所有资源的AssetBundle Name.
2.	分别打包Windows，Android和IOS平台下需要的AssetBundle包.因为项目需求，这里我打包的目录是指定的Assets外面，后面通过代码把资源copy到StreamingAssets下面。
3.	对比两次打包的AssetBundleManifest的文件差异，找出多余资源删除，并删除空的文件夹.
4.	AssetBundle资源复制到StreamingAssets下面.

二.加载AssetBundle

使用WWW流加载AssetBundle，加载的时候注意加载依赖关系。
