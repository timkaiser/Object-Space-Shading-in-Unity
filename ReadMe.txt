This is my bachelor thesis titled "Object Space Shading in Unity"
Included here are three parts of the thesis: 

- The first one is the PDF version of the written part of my thesis, It's the file with the name
	"Tim_Kaiser-Bachelor_Thesis-Object_Space_Shading.pdf".
- The second one is the unity project that is part of this thesis. It's in the folder titled "[Unity]_Project"
	
- The third one is the evaluation of the test data in Python. This is in the folder "[Python]_Evaluation".
	This folder includes all the data and a Jupyter notebook. This can be open using Jupyter.



How to open the Unity project:
1) Start Unity (ideally version 2018.2.10f1, but any version of 2018 or higher should work) or Unity Hub
2) Click "Edit" -> "Open Project..." in Unity or "Open" in Unity Hub
3) Select the folder "[Unity]_Project" and click open


How to see all features:
There are 3 different demo scenes in this project. They can be selected from the folder "Scenes" in the Unity editor project tab.
These scenes are: Earth, SponzaDemo and lostEmpire Demo. All other scenes where used for the test that were evaluated in the Python
project and descirbed in the "Evaluation" section of my thesis.
The code can be seen in the "Resources/Scripts" and "Resources/Shader" folder. especially relevant is the "Pipeline_OSS" script.
See all textures associated with one object can be examined via the Unity inspector, but this is a bit difficult since my pipeline 
broke the preview window. It still can be done following theses steps:
1) Select a object in the scene heirachy tab (child objects of either sponza or lostEmpire)
2) Click the expand the material tab in the inspector (should be the only expandeable tab visible)
3) If this is not possible click right above the lower dark grey part of the inspector window, where the curor turns into a double
	arrow and try step 3) again
4) Chose and click the texture you wnat to see

