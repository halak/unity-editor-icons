@echo off

FOR /R "icons\small" %%F in (.) DO ( "mogrify" -filter Lanczos2 -resize "32x32>" -quality 95 -strip "%%F\*.png" )

PAUSE