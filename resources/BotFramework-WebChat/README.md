# Microsoft Bot Framework Web Chat

This is a fork of the Microsoft Bot Framework (BF) Web Chat. It was made to include support for Custom Speech.

BF Web Chat uses the (Speech to Text SDK)[https://github.com/Azure-Samples/SpeechToText-WebSockets-Javascript] which was (missing support for Custom Speech)[https://github.com/Azure-Samples/SpeechToText-WebSockets-Javascript/issues/81]. To solve this, a custom build of the Speech to Text SDK was produced including the (necessary changes to add support for Custom Speech)[https://github.com/Azure-Samples/SpeechToText-WebSockets-Javascript/pull/82/files] and added to BF Web Chat. The modified version of BF Web Chat was then built. 

To recreate, follow these steps:
1) $ npm install
2) unzip the folder microsoft-speech-browser-sdk.zip 
3) go to the node_modules folder, find the folder microsoft-speech-browser-sdk and replace it with the unzipped folder
4) create a custom build of BF Web Chat by running: npm run prepublish

Questions? Email David Q: davidq@zarmada.com

[MIT License](/LICENSE)
