
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <windows.h>

#define VERBOSE_PRINT

void		read_header(FILE *fd, uint32_t *fileNb, uint32_t *align, uint16_t **listIDX, uint32_t **listOffset) {
	uint8_t		magicNb[8];
	uint8_t		cmp[] = { 0x41, 0x46, 0x53, 0x32, 0x01, 0x04, 0x02, 0x00 };
	uint32_t	offset;
	int			ret;

	ret = fread(magicNb, 1, 8, fd);
	if (memcmp(magicNb, cmp, 8) || ret != 8) {
		printf("Error: Bad magic number\n");
		exit(EXIT_FAILURE);
	}
	offset = ret;

	ret = fread(fileNb, 1, 4, fd);
	if (ret != 4) {
		printf("Error: Can\'t parse the header (0x%08d)\n", offset);
		exit(EXIT_FAILURE);
	}
	offset += ret;

	ret = fread(align, 1, 4, fd);
	if (ret != 4) {
		printf("Error: Can\'t parse the header (0x%08d)\n", offset);
		exit(EXIT_FAILURE);
	}
	offset += ret;

	*listIDX = malloc(*fileNb * sizeof(**listIDX));
	ret = fread(*listIDX, 2, *fileNb, fd);
	if (ret != (int)(*fileNb)) {
		printf("Error: Can\'t parse the header (0x%08d)\n", offset);
		exit(EXIT_FAILURE);
	}
	offset += (ret * 2);

	*listOffset = malloc((*fileNb + 1) * sizeof(**listOffset));
	ret = fread(*listOffset, 4, *fileNb + 1, fd);
	if (ret != (int) (*fileNb + 1)) {
		printf("Error: Can\'t parse the header (0x%08d)\n", offset);
		exit(EXIT_FAILURE);
	}
	offset += (ret * 4);

	return ;
}

void		extract_files(char *path, FILE *fd, uint32_t fileNb, uint32_t align, uint16_t *listIDX, uint32_t *listOffset) {
	char		filename[1024];
	char		dirname[1024];
	uint8_t		*buf;
	uint32_t	padding;
	int			ret;
	uint32_t	i;
	FILE		*file;


	sprintf(dirname, "%s_extracted_files", path);
	CreateDirectory(dirname, NULL);

	for (i = 0 ; i < fileNb ; i++) {
		sprintf(filename, "%s\\%08X.bin", dirname, listIDX[i]);
		padding = listOffset[i];
		listOffset[i] += (align - 1) - ((listOffset[i] - 1) % align);
		padding = listOffset[i] - padding;
		if (padding > 0) {
			buf = malloc(padding * sizeof(*buf));
			ret = fread(buf, 1, padding, fd);
			if (ret != (int) padding) {
				printf("Error: Archive may be corrupt\n");
				exit(EXIT_FAILURE);
			}
			free(buf);
		}
		buf = malloc((listOffset[i + 1] - listOffset[i]) * sizeof(buf));
		ret = fread(buf, 1, listOffset[i + 1] - listOffset[i], fd);
		if (ret != (int) (listOffset[i + 1] - listOffset[i])) {
			printf("Error: Unable to find %08X.bin\n", listIDX[i]);
			exit(EXIT_FAILURE);
		}

		file = fopen(filename, "wb");
		if (file == NULL) {
			perror("Error");
 			exit(EXIT_FAILURE);
 		}
 		ret = fwrite(buf, sizeof(*buf), listOffset[i + 1] - listOffset[i], file);
		if (ret != (int) (listOffset[i + 1] - listOffset[i])) {
			printf("Error: Unable to write %08X.bin to the disk\n", listIDX[i]);
			exit(EXIT_FAILURE);
		}
		fclose(file);
		free(buf);
#ifdef VERBOSE_PRINT
		printf("%08X.bin successfully extracted (%d bytes long)\n", listIDX[i], ret);
#endif // VERBOSE_PRINT
	}

	return ;
}

int			main(int argc, char **argv) {
	FILE		*fd;
	uint32_t	fileNb;
	uint32_t	align;
	uint16_t	*listIDX;
	uint32_t	*listOffset;
	int			i;

	if (argc == 1) {
		printf("Usage: %s <files>\n", argv[0]);
		exit(EXIT_FAILURE);
	}

	for (i = 1 ; i < argc ; i++) {
		fd = fopen(argv[i], "rb");
		if (fd == NULL) {
			printf("Error: Unable to open %s", argv[i]);
 			exit(EXIT_FAILURE);
		}
		read_header(fd, &fileNb, &align, &listIDX, &listOffset);
		extract_files(argv[i], fd, fileNb, align, listIDX, listOffset);
		fclose(fd);
		free(listIDX);
		free(listOffset);
	}

	return (EXIT_SUCCESS);
}
