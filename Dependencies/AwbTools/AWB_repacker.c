
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <unistd.h>
#include <fcntl.h>

#define VERBOSE_PRINT

void		write_file(uint8_t *data, uint32_t size) {
	FILE		*file;
	uint32_t	ret;

	file = fopen("OUT.AWB", "wb");
	if (file == NULL) {
			printf("unable to create new file");
 			exit(EXIT_FAILURE);
	}
	ret = fwrite(data, sizeof(*data), size, file);
	if (ret != size) {
		printf("an unknown error may have occurred");
		exit(EXIT_FAILURE);
	}

	fclose(file);

#ifdef VERBOSE_PRINT
	printf("archive successfully created (%d bytes long)\n", size);
#endif // VERBOSE_PRINT

	return ;
}

uint32_t	*get_files_size(char **files, uint32_t fileNumber) {
	struct stat	buf;
	int			fd;
	uint32_t	*size;
	uint32_t	i;

	size = malloc(fileNumber * sizeof(*size));
	for (i = 0 ; i < fileNumber ; i++) {
		fd = fopen(files[i], O_RDONLY);
		if (fd == -1) {
			printf("unable to open %s", files[i]);
 			exit(EXIT_FAILURE);
		}
		if ((fstat(fd, &buf) != 0) || (!S_ISREG(buf.st_mode))) {
			printf("unable to retrieve information about %s", files[i]);
 			exit(EXIT_FAILURE);
		}
		size[i] = buf.st_size;
		fclose(fd);
	}

	return (size);
}

uint32_t	create_header(uint8_t **header, uint32_t fileNumber) {
	int8_t		dummy[] = { 0x41, 0x46, 0x53, 0x32, 0x01, 0x04, 0x02, 0x00 };
	int32_t		alignment = 32;
	uint16_t	fileIndex[fileNumber];
	uint32_t	headerSize;
	uint8_t		*offset;
	uint32_t	i;

	for (i = 0 ; i < fileNumber ; i++) {
		fileIndex[i] = i;
	}

	headerSize = 16 + sizeof(fileIndex) + ((fileNumber + 1) * 4);
	headerSize += 31 - ((headerSize - 1) % 32);

	*header = calloc(headerSize, sizeof(*header));

	offset = *header;
	memcpy(offset, dummy, sizeof(dummy));
	offset += sizeof(dummy);
	memcpy(offset, &fileNumber, sizeof(fileNumber));
	offset += sizeof(fileNumber);
	memcpy(offset, &alignment, sizeof(alignment));
	offset += sizeof(alignment);
	for (i = 0 ; i < fileNumber ; i++) {
		memcpy(offset, &fileIndex[i], sizeof(fileIndex[i]));
		offset += sizeof(fileIndex[i]);
	}

#ifdef VERBOSE_PRINT
	printf("header generated (%d bytes long)\n", headerSize);
#endif // VERBOSE_PRINT

	return (headerSize);
}

void		add_offsets(uint8_t *header, uint32_t headerSize, uint32_t *fileSizeTab, int fileNumber) {
	uint32_t	offset;
	int			i;

	offset = 16 + (fileNumber * 2);
	headerSize = 16 + (fileNumber * 2) + ((fileNumber + 1) * 4);

	for (i = 0 ; i < fileNumber ; i++) {
		memcpy(header + offset, &headerSize, 4);
		headerSize += 31 - ((headerSize - 1) % 32);
		headerSize += fileSizeTab[i];
		offset += 4;
	}
	memcpy(header + offset, &headerSize, 4);

	return ;
}

uint32_t	add_file(uint8_t **data, char *path, uint32_t dataSize, uint32_t fileSize) {
	FILE		*stream;
	uint8_t		*buf;
	uint8_t		*newfile;
	int			ret;
	int			padding;

	stream = fopen(path, "rb");
	if (stream == NULL) {
		printf("unable to open %s", path);
 		exit(EXIT_FAILURE);
	}

	padding = 31 - ((fileSize - 1) % 32);
	buf = calloc(fileSize + padding, sizeof(*buf));

	ret = fread(buf, 1, fileSize, stream);
	if (ret == -1) {
		printf("unable to read %s", path);
		exit(EXIT_FAILURE);
	}

	if (ret != (int) fileSize) {
		printf("unknown error while reading %s", path);
		exit(EXIT_FAILURE);
	}

	newfile = malloc((dataSize + fileSize + padding) * sizeof(*newfile));
	memcpy(newfile, *data, dataSize);
	memcpy(newfile + dataSize, buf, fileSize + padding);

	fclose(stream);
	free(*data);
	free(buf);

	*data = newfile;

#ifdef VERBOSE_PRINT
	printf("%s added successfully (%d bytes long)\n", path, fileSize);
#endif // VERBOSE_PRINT

	return (dataSize + fileSize + padding);
}

int			main(int argc, char **argv) {
	uint8_t		*data;
	uint32_t	dSize;
	uint32_t	*fileSizeTab;
	int			i;

	if (argc == 1) {
		printf("usage: %s <files>\n", argv[0]);
		exit(EXIT_FAILURE);
	}
	dSize = create_header(&data, argc - 1);
	fileSizeTab = get_files_size(argv + 1, argc - 1);
	add_offsets(data, dSize, fileSizeTab, argc - 1);
	for (i = 0 ; i + 1 < argc ; i++) {
		dSize = add_file(&data, argv[i + 1], dSize, fileSizeTab[i]);
	}
	write_file(data, dSize);
	free(data);
	free(fileSizeTab);

	return (EXIT_SUCCESS);
}
