function loadContinuousFileReference() {
    const pickerOpts = {
        types: [
            {
                description: "SVG Images",
                accept: {
                    "image/*": [".svg"],
                },
            },
        ],
        excludeAcceptAllOption: true,
        multiple: false,
        id: "continuous"
    };

    return window.showOpenFilePicker(pickerOpts);
}

async function loadFileStringContent(fileReference) {
    if (fileReference.length == 0) {
        console.log("No file(s) loaded, returning empty string");
        return "";
    }
    else {
        const file = await fileReference[0].getFile();
        const contents = await file.text();
        return contents;
    }
}