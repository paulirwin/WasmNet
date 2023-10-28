;; invoke: convert
;; expect: (f32:-42)

(module
    (func (export "convert") (result f32)
        i32.const -42
        f32.convert_i32_s
    )
)