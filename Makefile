tds.exe: Main.cs CPU.cs Processor.cs Cache.cs Protocol.cs Extensions.cs BerkleyCache.cs BerkleyProtocol.cs ThreeStateBasicProtocol.cs ThreeStateBasicCache.cs
	mcs $^ -out:$@
