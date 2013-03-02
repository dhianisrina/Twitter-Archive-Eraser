Twitter Archive Eraser
======================

Remove the oldest tweets from your account using Twitter Archive Eraser, a .NET app for bulk tweets deletion using the Twitter archive.

More details about how to use this tool to wipe out your Twitter archive [http://www.martani.net/2013/02/wipe-your-oldest-tweets-using-twitter.html](http://www.martani.net/2013/02/wipe-your-oldest-tweets-using-twitter.html)
_______
**Update:**

Ver 2.0 enables the use of the *.js archive files after the Twitter update the the archive structure.
_______

Download executable
-------------------

You can download a working standalone version from here: [Twitter Archive Eraser.zip](Twitter%20Archive%20Eraser.zip?raw=true)
Or, download the installer which will install all the required software (.NET 4.0 etc.): [Twitter Archive Eraser Setup.zip](Twitter%20Archive%20Eraser%20Setup.zip?raw=true)


Building the code using Visual Studio
----------------------------

If you wish to buid the code yourself, you need to create a Twitter application here [https://dev.twitter.com](https://dev.twitter.com) and provide the `twitterConsumerKey` and `twitterConsumerSecret` parameters in `App.config`.
<pre>
	&lt;add key="twitterConsumerKey" value=""/>
	&lt;add key="twitterConsumerSecret" value=""/>
</pre>
