;; invoke: store-load
;; expect: (f32:42)

(module
    (memory 1)
    (func (export "store-load") (result f32)
        i32.const 0 ;; dynamic offset
        f32.const 42.0
        f32.store offset=64
        i32.const 0 ;; dynamic offset
        f32.load offset=64
    )
)