import tensorflow as tf

def to_savedmodel_tfv1(h5_model_path, output_folder):
    """
    Convert Keras HDF5 model to Tensorflow SavedModel format
    Used for Tensorflow v1.x
    @h5_model_path: path to hdf5 model (not weights)
        If you only have h5 weights, you must create a model structure
        and load the weights into that model structure
        Then use `model.save(h5_model_path)` to save it to HDF5 model
    @output_folder: an empty folder to store output (model in pb format)
    """
    model = tf.keras.models.load_model(h5_model_path)

    builder = tf.saved_model.builder.SavedModelBuilder(output_folder)
    signature = tf.saved_model.signature_def_utils.predict_signature_def(
        inputs={'image': model.input}, outputs={'scores': model.output})

    from keras import backend as K
    builder.add_meta_graph_and_variables(
        sess=K.get_session(),
        tags=[tf.saved_model.tag_constants.SERVING],
        signature_def_map={
            tf.saved_model.signature_constants.DEFAULT_SERVING_SIGNATURE_DEF_KEY:
                signature
        })
    builder.save()


def to_savedmodel_tfv2(h5_model_path, output_folder):
    """
    Convert Keras HDF5 model to Tensorflow SavedModel format
    Used for Tensorflow v2.x
    @h5_model_path: path to hdf5 model (not weights)
    @output_folder: an empty folder to store output (model in pb format)
    """
    model = tf.keras.models.load_model(h5_model_path)
    tf.saved_model.save(model, output_folder)
