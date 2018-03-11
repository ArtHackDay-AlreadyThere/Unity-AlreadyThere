#ifndef __SHAPE_DRAW_DATA__
#define __SHAPE_DRAW_DATA__

// �}�`�`��p�f�[�^
struct ShapeDrawData
{
	float3 position;	// ���W
	int vertexCount;    // ���_��
	float number;		// �ԍ�
	int seq;            // �V�[�P���X
	int blurCount;      // �c����
	float size;         // �T�C�Y
	float hashFloat;	// �n�b�V��
	uint id;			// ������ID(�N��������̘A��)
	float4 color;		// �F
	float fadeDuration;  // �����鎞�̎���
	bool isSpecial;      // ���ʂȑ����悩�H
};
#endif
