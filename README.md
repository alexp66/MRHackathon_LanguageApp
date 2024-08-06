A Mixed-Reality demo application for language learning. 
Submission for 2024 Meta Presence Platform hackathon.
Submission Page: https://devpost.com/software/language-learning-memory-palace?ref_content=my-projects-tab&ref_feature=my_projects

For development, some of the frameworks, APIs and SDKs that we used and how they were implemented are as follows:

Spatial Anchors: The foundation of Loci. A powerful and reliable feature for mixed reality experiences, opening up a host of possibilities for app creation using Quest Passthrough.

Interaction SDK: Poke Interactions, Hand Grab Interactions, Synthetic Hands, and Custom hand pose detection. We wanted an index finger pointing gesture for distance selection, and created it using Meta's Custom hand pose detection capabilities, which were pretty clearly explained in the documentation.

MLCommons Multilingual Spoken Words Dataset (open): This open dataset contains thousands of audio clips for native pronunciations of words. For this project, we included a database of some 17,000 spanish words and their english translations in text format, which can be searched by the user on the flashcard. For the pronunciation audio, as this was a small-scale demo, we hand-picked 9 common household words (the words in Randomize mode), and included 4 native pronunciations for each. We deliberately included a mixture of female, male, young and old voices as much as possible to enhance listening practice. We were inspired by the ability to listen to multiple native pronunciations at will, as understanding native speakers is something we have struggled with in our language learning journey.

Wit.AI via Meta Voice SDK (Dictation): We leveraged Meta's Voice SDK's Dictation feature to allow the voice-search during flashcard creation. This is activated by a flashcard button press, and returns the first word recognized by the audio input, comparing it against the MLCommons dataset which returns the Spanish translation. One challenge was that there are many duplicate words in the dataset, so voice dictation alone isn't sufficient to get perfect translation accuracy. Extending this feature to use Voice SDK's more advanced AI-powered voice interpretation could take it to the next level.

Visual Design Tools: Blender, Figma, DOTween (animations) among others were used to craft the visual language of Loci.
