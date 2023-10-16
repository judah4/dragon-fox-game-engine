# spinny_blobs
these are the spinny (pride) blobs of life  
credit to https://misskey.io/@nure500 for creating the o.g. low poly spinny blob cat! (https://misskey.io/notes/9cyq9fc0ym)

## where the daMned emojos are
they shall be in /render/cat/trimmed (or /render/fox/trimmed for the foxes)  
  
## the (manual) pipeline
what i do is:

1. render the animation as a PNG sequence
2. because im lazy i just used ezgif.com to put the gifs together  
a. after uploading the sequence i check 'use global colormap' and 'don't stack frames' before clicking the Make a GIF! button  
b. then i go to 'speed' and change the speed to 999%  
c. finyally i go to effects and tick 'black' under 'replace colour with transparency'
3. then i use `for i in *.gif; do convert "$i" -coalesce -trim -layers trim-bounds "$i"; done` to bulk-trim 'em. (thank u very much elke for that command)
