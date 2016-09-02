MKDIR UpdatingFiles
MKDIR UpdatingFiles\Config
MKDIR UpdatingFiles\Resources
MKDIR UpdatingFiles\Resources\Countries

DEL Release.ZIP /f /s /q

COPY AppLimit.CloudComputing.SharpBox.dll UpdatingFiles\AppLimit.CloudComputing.SharpBox.dll
COPY AppLimit.CloudComputing.SharpBox.ExperimentalProvider.dll UpdatingFiles\AppLimit.CloudComputing.SharpBox.ExperimentalProvider.dll
COPY AppLimit.CloudComputing.SharpBox.MockProvider.dll UpdatingFiles\AppLimit.CloudComputing.SharpBox.MockProvider.dll
COPY DotNetOpenAuth.dll UpdatingFiles\DotNetOpenAuth.dll
COPY DropBoxTokenIssuer.exe UpdatingFiles\DropBoxTokenIssuer.exe
COPY DropNet.dll UpdatingFiles\DropNet.dll
COPY Google.Apis.Authentication.OAuth2.dll UpdatingFiles\Google.Apis.Authentication.OAuth2.dll
COPY Google.Apis.dll UpdatingFiles\Google.Apis.dll
COPY Google.Apis.Drive.v2.dll UpdatingFiles\Google.Apis.Drive.v2.dll
COPY HtmlAgilityPack.dll UpdatingFiles\HtmlAgilityPack.dll
COPY log4net.dll UpdatingFiles\log4net.dll
COPY Newtonsoft.Json.Net35.dll UpdatingFiles\Newtonsoft.Json.Net35.dll
COPY Newtonsoft.Json.Net35.xml UpdatingFiles\Newtonsoft.Json.Net35.xml
COPY Newtonsoft.Json.Net40.dll UpdatingFiles\Newtonsoft.Json.Net40.dll
COPY Newtonsoft.Json.Silverlight.dll UpdatingFiles\Newtonsoft.Json.Silverlight.dll
COPY RestSharp.dll UpdatingFiles\RestSharp.dll
COPY RestSharp.xml UpdatingFiles\RestSharp.xml
COPY WordTraining.exe UpdatingFiles\WordTraining.exe
COPY WordTraining.exe.config UpdatingFiles\WordTraining.exe.config
COPY WpfAnimatedGif.dll UpdatingFiles\WpfAnimatedGif.dll

COPY Google.Apis.Authentication.OAuth2.pdb UpdatingFiles\Google.Apis.Authentication.OAuth2.pdb
COPY Google.Apis.Authentication.OAuth2.xml UpdatingFiles\Google.Apis.Authentication.OAuth2.xml
COPY Google.Apis.pdb UpdatingFiles\Google.Apis.pdb
COPY Google.Apis.xml UpdatingFiles\Google.Apis.xml
COPY HtmlAgilityPack.pdb UpdatingFiles\HtmlAgilityPack.pdb
COPY HtmlAgilityPack.xml UpdatingFiles\HtmlAgilityPack.xml
COPY WordTraining.pdb UpdatingFiles\WordTraining.pdb
COPY WpfAnimatedGif.xml UpdatingFiles\WpfAnimatedGif.xml

XCOPY Config UpdatingFiles\Config
XCOPY Resources UpdatingFiles\Resources
XCOPY Resources\Countries UpdatingFiles\Resources\Countries

"C:\Program Files\WinRAR\WinRAR.exe" a -r Release.ZIP UpdatingFiles
COPY UpdatingFiles\Release.ZIP Release.ZIP

RMDIR UpdatingFiles /s /q
REM WordTraining.exe /upload
EXIT /B 0