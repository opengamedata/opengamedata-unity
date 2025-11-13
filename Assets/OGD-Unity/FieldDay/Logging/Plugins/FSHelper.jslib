var OGDFSLib = {

    /**
     * Syncs the file system.
     */
    OGDLog_SyncFiles: function () {
        JS_FileSystem_Sync();
    }
}

mergeInto(LibraryManager.library, OGDFSLib);