```
TABRTreset 1.0.0.0
TABRTreset is a tool to crack encryption mechanism in game They Are Billions.
The initial purpose of it is to reset research tree. In fact its fullname is They Are Billions research tree reset.
Currently works on Steam Editon V.1.1.3. It ensures no backward compatibility.

Usage:

  reset <save_name_or_path>         read the save, delete all researched technology, add coresponding research points 
                                    and write back to save.

  gencheck <save_name_or_path>      generate .zxcheck for specific file

  genpswd                           generate all password of saves(.zxsave) in savepswd.json, and generate all password 
                                    of game data file(.dat) in datpswd.json

  unpacksave <save_name_or_path>    unzip save(.zxsav) to a same name folder

  unpackdat <save_name_or_path>     unzip data(.dat) to a same name folder

  packsave <folder_name_or_path>    zip folder to a same name .zxsav file with proper password, and generate .zxcheck

  packdat <folder_name_or_path>     zip folder to a same name .dat file with proper password

  --save_folder                     specify the path to save folder, default: (MyDocuments)\My Games\They Are
                                    Billions\Saves\

  --tab_folder                      specify the path to TAB game folder, default: (Steam installtion
                                    path)\steamapps\common\They Are Billions\
```