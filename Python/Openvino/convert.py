import openvino as ov
model_path = " "
ir_path = " "
ov_model = ov.convert_model(model_path)
ov.save_model(ov_model, ir_path)

