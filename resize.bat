@echo off

FOR /R "icons\small" %%F in (.) DO ( "mogrify" -resize "32x32>" "%%F\*.png" )