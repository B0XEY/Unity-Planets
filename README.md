![Static Badge](https://img.shields.io/badge/Verson-2022.3.22f1-5300EB?style=for-the-badge&logo=Unity)
[![Static Badge](https://img.shields.io/badge/Code%20Quality-b-green?style=for-the-badge&logo=codacy)](https://app.codacy.com/gh/B0XEY/Unity-Planets/dashboard?utm_source=gh&utm_medium=referral&utm_content=&utm_campaign=Badge_grade)
[![Static Badge](https://img.shields.io/badge/Version-0.2.5b-blue?style=for-the-badge)](https://github.com/B0XEY/Unity-Planets/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=for-the-badge)](https://opensource.org/licenses/MIT)

> [!IMPORTANT]
> This Code is unfinished and not deemed production-ready.
> [Download a Small Demo Here](https://github.com/B0XEY/Unity-Planets/releases/tag/0.2.5)
> 
> More Update Soon™️

# Project Overview
Welcome to **Unity-Planets** where the coding skills come into play. The planets are fully customizable from the noise to the Terriforming that you do. I used a _ModTree_[^3] that will save all changes the player makes. Planets are boring with just terrain so why not mix it up and add some _trees_ and _grass_? All of these are aligned with the planet and thanks to not-so-random points we can spawn all the objects in the same position every time even when you load and unload the Node. This code also uses _Unity's Jobs/Burst systems_ to speed up the generation allowing for fast Update times and a higher average FPS. All the systems of the octree come together to create a wonderful place for the user to explore and make their own.

## Features
- uses jobs / brust for faster performance.
- Uses Octree
- ![ezgif com-crop](https://github.com/B0XEY/Unity-Planets/assets/94720404/36066c3d-04d4-4b35-8301-1211b252a285)
- Noise[^1] Layers
- Marching Cubes [^2]
- Customizable Planet Material via Scriptable Object
- Terraforming?
- Large planets
- Example scene

## To-Do
- [ ] Planet caves
- [x] ~~Planet Chunk Mesh Gaps~~ (Almost Complete but not noticable so I'm crossing it off)
- [x] ~~Planet Roation with working chunk Generation~~
- [x] ~~Terraforming Checks~~
- [ ] Object Spawning
- [x] ~~Player Controller / gravity~~
- [x] ~~Walkable Demo~~

## Contributing
I appreciate your interest in contributing to the project. Whether you're a seasoned developer or just getting started, your contributions are highly valued. To contribute, you can follow these steps:

**Fork-it:** If you have improvements, bug fixes, or new features in mind, fork the repository and create a branch for your changes.

**Download and Modify Code:** Make your changes in the forked repository and test them thoroughly. Once you're satisfied, submit a pull request.

**Join the Discussion:** Share your ideas, feedback, or questions in the discussion section. You can also propose new features or improvements. Your insights are crucial to the growth and refinement of the project.

**Create a New Post:** If you have non-code-related contributions, such as documentation updates, tutorials, or general suggestions, feel free to create a new post in the discussion idea category. We welcome diverse perspectives and ideas!


## Images
![Screenshot 2024-06-30 163516](https://github.com/B0XEY/Unity-Planets/assets/94720404/b70a6239-8f76-42c5-9ff2-0628a06b8a6d)
![Screenshot 2024-06-30 211100](https://github.com/B0XEY/Unity-Planets/assets/94720404/2030b163-b3c4-4a22-8b1a-b64d826d36fa)
![Screenshot 2024-07-02 153135](https://github.com/B0XEY/Unity-Planets/assets/94720404/6e3845a7-bd62-4272-875d-845d416de481)
![Screenshot 2024-07-02 155703](https://github.com/B0XEY/Unity-Planets/assets/94720404/dd9e31db-4dee-4907-be7f-2518a40e0435)


> [!NOTE]
>*Atmosphere shader IS included (It's not mine but here is the [link](https://github.com/sinnwrig/URP-Atmosphere?tab=readme-ov-file))*


## License
MIT License

Copyright (c) 2024 Boxey Dev

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WIT HOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

[^1]: These planets use 3D noise to shape the terrain. This is not the best option because of the lack of control. Thankfully [unbeGames's noise algorithm](https://github.com/unbeGames/noise.git) not only is jobs/ burst enabled but makes layered noise much faster than using normal librays.
[^2]: The Terrian is made using the marching cube algorithm. The base of my code comes from this [video series](https://www.youtube.com/watch?v=dTdn3CC64sc&list=PLVsTSlfj0qsWt0qafrT6blp5yvchzO4ee) with was a big help with the generation. I have made minor tweaks and changes to speed up the generation. [Scrawk's Marching Cubes Project](https://github.com/Scrawk/Marching-Cubes)
[^3]: The idea of the mod tree came from [here](https://josebasierra.gitlab.io/VoxelPlanets) after looking at solution to save memory.
