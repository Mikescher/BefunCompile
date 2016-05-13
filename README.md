![](https://raw.githubusercontent.com/Mikescher/BefunUtils/master/README-FILES/icon_BefunCompile.png) BefunCompile
========

BefunGen is a Befunge-93 compiler. *(Like all of my Befunge-93 content the Befunge-93 [size restriction](https://github.com/catseye/Befunge-93/blob/master/doc/Befunge-93.markdown) is removed, programs can be any size - much like in Befunge-98)*
There [are](https://github.com/nilp0inter/awkfunge) [many](https://github.com/serprex/Befunge) [general](http://madflame991.github.io/befunjit/src/visualizer/visualizer.html) [compiler](http://quadium.net/funge/tbc/) out there. But mine is not one of them.
It can only compile Befunge-code which does **not** modify its own source code *(its ok to modify other, non interpreted, cells)*. But nearly every program in Befunge, handmade or automatically generated does not change its own code while running.

So no, this is not a solution to the main challenge of Befunge, but a nice way to generate executables out of your Befunge code.


Usage
=====

BefunCompile itself is a console program. Call it with `help` or without parameters to see a list of all possible arguments.  
BefunCompile is able to compile multiple files after each other when you supply it with an file pattern.

To compile all the code in my [Project-Euler](https://github.com/Mikescher/Project-Euler_Befunge) Repository I used this command:

~~~
"Path\To\BefunCompile.exe" ^
    --file="processed\*.b93" ^
    --languages=c;cs ^
    --out="compiled\{l}\{f}.{le}" ^
    --format ^
    --safestack ^
    --safegrid ^
    --override ^
    --unsafe
~~~

after that you can compile the resulting code with the respective compilers:

~~~
csc Euler_Problem-001.cs
gcc Euler_Problem-001.c
python Euler_Problem-001.py
~~~

Examples
========

My repository [Mikescher/Project-Euler_Befunge](https://github.com/Mikescher/Project-Euler_Befunge) contains a lot of examples. Every Befunge source code there is also as compiled C/C#/Python source code available.

Download
========

You can download the binaries from my website [www.mikescher.com](http://www.mikescher.com/programs/view/BefunUtils).   
Or compile them yourself by forking this repository

Set Up
======

This program was developed under Windows with Visual Studio.

You don't need other [BefunUtils](https://github.com/Mikescher/BefunUtils) projects to run this.  
Theoretically you can only clone this repository and run it.  
But it could be useful to get the whole BefunUtils solution like described [here](https://github.com/Mikescher/BefunUtils/blob/master/README.md)  
Especially BefunDebug could be useful for testing.


Screenshots
==========
BefunCompile:  
![](https://raw.githubusercontent.com/Mikescher/BefunUtils/master/README-FILES/BefunCompile_Main_example.png)

BefunCompile (Graph display of [Euler_Problem-002](https://github.com/Mikescher/Project-Euler_Befunge/blob/master/Euler_Problem-002.b93) Level **0**) *(via [BefunDebug](https://github.com/Mikescher/BefunDebug))*:  
![](https://raw.githubusercontent.com/Mikescher/BefunUtils/master/README-FILES/BefunCompile_Graph-0_example.png)

BefunCompile (Graph display of [Euler_Problem-002](https://github.com/Mikescher/Project-Euler_Befunge/blob/master/Euler_Problem-002.b93) Level **2**) *(via [BefunDebug](https://github.com/Mikescher/BefunDebug))*:  
![](https://raw.githubusercontent.com/Mikescher/BefunUtils/master/README-FILES/BefunCompile_Graph-2_example.png)

BefunCompile (Graph display of [Euler_Problem-002](https://github.com/Mikescher/Project-Euler_Befunge/blob/master/Euler_Problem-002.b93) Level **3**) *(via [BefunDebug](https://github.com/Mikescher/BefunDebug))*:  
![](https://raw.githubusercontent.com/Mikescher/BefunUtils/master/README-FILES/BefunCompile_Graph-3_example.png)

BefunCompile (Graph display of [Euler_Problem-002](https://github.com/Mikescher/Project-Euler_Befunge/blob/master/Euler_Problem-002.b93) Level **5**) *(via [BefunDebug](https://github.com/Mikescher/BefunDebug))*:  
![](https://raw.githubusercontent.com/Mikescher/BefunUtils/master/README-FILES/BefunCompile_Graph-5_example.png)

