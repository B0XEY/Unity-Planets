![Static Badge](https://img.shields.io/badge/Verson-2022.3.6f1-5300EB?style=for-the-badge&logo=Unity)
[![Static Badge](https://img.shields.io/badge/Version-0.0.1a-blue?style=for-the-badge)](https://github.com/B0XEY/Unity-Planets/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=for-the-badge)](https://opensource.org/licenses/MIT)
![Static Badge](https://img.shields.io/badge/CSharp-B80000?style=for-the-badge&logo=csharp)

# Unity Planets - URP
> [!IMPORTANT]
> This Code is unfinished and not personly deemed production ready.

 


## Features
- uses jobs / brust for faster performance.
- Uses Octree
- ![ezgif com-crop](https://github.com/B0XEY/Unity-Planets/assets/94720404/36066c3d-04d4-4b35-8301-1211b252a285)
- Noise[^1] Layers
- Marching Cubes [^2]
- Customizable Planet Material via Scriptable Object
- Terraforming
- Water Shader (3D noise-based waves)
- Large planets
- Example scene

## Contributing
🚀I appreciate your interest in contributing to the project. Whether you're a seasoned developer or just getting started, your contributions are highly valued. To contribute, you can follow these steps:

**Fork-it:** If you have improvements, bug fixes, or new features in mind, fork the repository and create a branch for your changes.

**Download and Modify Code:** Make your changes in the forked repository and test them thoroughly. Once you're satisfied, submit a pull request.

**Join the Discussion:** Share your ideas, feedback, or questions in the discussion section. You can also propose new features or improvements. Your insights are crucial to the growth and refinement of the project.

**Create a New Post:** If you have non-code-related contributions, such as documentation updates, tutorials, or general suggestions, feel free to create a new post in the discussion idea category. We welcome diverse perspectives and ideas!


## Images
![Screenshot 2023-11-17 230944](https://github.com/B0XEY/Unity-Planets/assets/94720404/b106ffe9-d030-4066-a341-f84f8b87d428)
![Screenshot 2023-11-27 125453](https://github.com/B0XEY/Unity-Planets/assets/94720404/a017d0a4-f75a-45f6-8dc6-94f0b5238aa5)
![image](https://github.com/B0XEY/Unity-Planets/assets/94720404/7238b250-0731-4556-b0cd-041353129042)
> [!CAUTION]
>*Atmosphere shader not included ([Link](https://assetstore.unity.com/packages/3d/environments/sci-fi/space-graphics-planets-124578))*

## License
MIT License

Copyright (c) 2023 Boxey Dev

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

[^1]: These planets use 3D noise to shape the terrain. This is not the best option because of the lack of control. Thankfully [unbeGames's noise algorithm](https://github.com/unbeGames/noise.git) not only is jobs/ burst enabled but makes layered noise much faster than using normal librays.
[^2]: The Terrian is made using the marching cube algorithm. The base of my code comes from this [video series](https://www.youtube.com/watch?v=dTdn3CC64sc&list=PLVsTSlfj0qsWt0qafrT6blp5yvchzO4ee) with was a big help with the generation. I have made minor tweaks and changes to speed up the generation. [Scrawk's Marching Cubes Project](https://github.com/Scrawk/Marching-Cubes)
