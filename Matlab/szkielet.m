I = imread('sz1.png');
BW = rgb2gray(I);
BW = imcomplement(BW);
figure(1)
imshow(imcomplement(BW));

BW2 = bwmorph(BW,'skel',Inf);
figure(2)
imshow(imcomplement(BW2));

BW3 = bwmorph(BW2,'spur',Inf);
figure(3)
imshow(imcomplement(BW3));


imwrite(imcomplement(BW),'water1.png')
imwrite(imcomplement(BW2),'water2.png')
imwrite(imcomplement(BW3),'water3.png')